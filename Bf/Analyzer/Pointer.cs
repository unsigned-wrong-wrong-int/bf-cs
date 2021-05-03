using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bf.Analyzer
{
   enum PointerState : byte
   {
      Initial,
      AfterLoop,
      StartOfLoop,
   }

   class Pointer
   {
      public Pointer? Next { get; private set; }

      readonly Context context;
      readonly PointerState state;
      bool isEndOfLoop;

      int maxOffset;
      int minOffset;
      int offset;

      readonly Dictionary<int /* offset */, Cell> cells;

      readonly Queue<(int offset, Command)> commands;

      public Pointer(Context context, PointerState state)
      {
         Next = null;
         this.context = context;
         this.state = state;
         isEndOfLoop = false;
         maxOffset = minOffset = offset = 0;
         cells = new();
         commands = new();
      }

      public Pointer() : this(new(false), PointerState.Initial) { }

      Cell GetCell(int pos = 0)
      {
         checked
         {
            pos += offset;
         }
         if (pos > maxOffset)
         {
            maxOffset = pos;
         }
         else if (pos < minOffset)
         {
            minOffset = pos;
         }
         if (!cells.TryGetValue(pos, out var cell))
         {
            cell = new(isZero:
               state == PointerState.AfterLoop
                  ? pos == 0
                  : state == PointerState.Initial
               );
            cells.Add(pos, cell);
         }
         return cell;
      }

      public void Increment() => GetCell().Increment();

      public void Decrement() => GetCell().Decrement();

      public void MoveRight() => _ = checked(++offset);

      public void MoveLeft() => _ = checked(--offset);

      public void Write()
      {
         context.PerformsIO = true;
         commands.Enqueue((offset, GetCell().Write()));
      }

      public void Read()
      {
         context.PerformsIO = true;
         commands.Enqueue((offset, GetCell().Read()));
      }

      public bool BeginLoop([NotNullWhen(true)] out Pointer? loopStart)
      {
         var cell = GetCell(offset);
         if (cell.IsZero)
         {
            loopStart = null;
            return false;
         }
         loopStart = new(new(cell.IsNonZero), PointerState.StartOfLoop);
         return true;
      }

      void AppendLoop(Pointer start, Pointer end)
      {
         Next = start;
         end.isEndOfLoop = true;
         end.Next = new(context, PointerState.AfterLoop);
      }

      void EnqueueCommands(Queue<(int offset, Command)> queue)
      {
         foreach (var (offset, command) in queue)
         {
            commands.Enqueue((this.offset + offset, command));
         }
      }

      bool ToInfiniteLoop(Pointer outer, Pointer loopEnd)
      {
         if (!context.PerformsIO)
         {
            outer.commands.Enqueue((outer.offset,
               Command.InfiniteLoop(context.IsConditional)));
            return context.IsConditional;
         }
         context.Repetition = Repetition.Infinite;
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool ToConditional(Pointer outer, Pointer loopEnd)
      {
         if (!context.IsConditional)
         {
            outer.EnqueueCommands(commands);
            foreach (var (pos, cell) in cells)
            {
               outer.GetCell(pos).Merge(cell);
            }
            if (Next is not null)
            {
               outer.AppendLoop(Next, loopEnd);
            }
            return true;
         }
         context.Repetition = Repetition.Once;
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool ToMultipliation(Pointer outer, Cell last, Pointer loopEnd)
      {
         if (last.GetDivisor() is not { } divisor)
         {
            outer.AppendLoop(this, loopEnd);
            return true;
         }

         // `cells[0]` is identical to `last`
         _ = cells.Remove(0);
         List<Step> steps = new();
         foreach (var (pos, cell) in cells)
         {
            if (cell.IsNoop)
            {
               continue;
            }
            if (cell.GetStep(pos) is not { } step)
            {
               goto leaveUnoptimized;
            }
            steps.Add(step);
         }

         var command = outer.GetCell().Load(divisor, out var multiplier);
         if (command is not null)
         {
            outer.commands.Enqueue((0, command));
            if (command.Type == CommandType.InfiniteLoop)
            {
               return false;
            }
         }
         foreach (var step in steps)
         {
            outer.GetCell(step.Offset).Add(step, multiplier, command);
         }
         outer.EnqueueCommands(commands);
         return true;

      leaveUnoptimized:
         // restore the removed cell
         cells.Add(0, last);
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool OptimizeLoop(Pointer outer, Pointer loopEnd)
      {
         context.Close(offset, outer.context);
         Cell last;
         if (offset == 0)
         {
            if (!cells.TryGetValue(0, out last!) || last.IsNonZero)
            {
               return ToInfiniteLoop(outer, loopEnd);
            }
            if (last.IsZero)
            {
               return ToConditional(outer, loopEnd);
            }
            if (Next is null && !context.PerformsIO)
            {
               return ToMultipliation(outer, last, loopEnd);
            }
         }
         else if (cells.TryGetValue(offset, out last!))
         {
            if (last.IsNonZero)
            {
               return ToInfiniteLoop(outer, loopEnd);
            }
            if (last.IsZero)
            {
               return ToConditional(outer, loopEnd);
            }
         }
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      public bool EndLoop(Pointer loopStart, Pointer loopEnd) =>
         loopStart.OptimizeLoop(this, loopEnd);

      void EmitBeginBlock(Builder builder)
      {
         if (state == PointerState.StartOfLoop)
         {
            if (context.IsConditional)
            {
               builder.BeginIf();
            }
            if (context.Repetition != Repetition.Once)
            {
               switch (context.Move)
               {
                  case PointerMove.Fixed:
                     builder.CheckUpperBound(maxOffset);
                     builder.CheckLowerBound(minOffset);
                     builder.BeginLoop();
                     return;
                  case PointerMove.Forward:
                     builder.CheckLowerBound(minOffset);
                     builder.BeginLoop();
                     builder.CheckUpperBound(maxOffset);
                     return;
                  case PointerMove.Backward:
                     builder.CheckUpperBound(maxOffset);
                     builder.BeginLoop();
                     builder.CheckLowerBound(minOffset);
                     return;
               }
               builder.BeginLoop();
            }
         }
         builder.CheckUpperBound(maxOffset);
         builder.CheckLowerBound(minOffset);
      }

      void EmitEndBlock(Builder builder)
      {
         builder.Move(offset);
         if (!isEndOfLoop)
         {
            return;
         }
         if (context.Repetition != Repetition.Once)
         {
            builder.EndLoop(context.Repetition == Repetition.Ordinary);
         }
         if (context.IsConditional)
         {
            builder.EndIf();
         }
      }

      void EmitCommands(Builder builder)
      {
         foreach (var (offset, cell) in cells)
         {
            builder.AddConst(offset, cell.Head.Value, cell.Head.Overwrite);
         }
         foreach (var (offset, command) in commands)
         {
            switch (command.Type)
            {
               case CommandType.Write:
                  if (command.Node is null)
                  {
                     builder.WriteConst(command.Value);
                     continue;
                  }
                  builder.Write(offset);
                  break;
               case CommandType.Read:
                  builder.Read(offset);
                  break;
               case CommandType.InfiniteLoop:
                  builder.InfiniteLoop(command.IsConditional);
                  break;
               default:
                  builder.Load(offset, command.ShiftRight);
                  if (command.Targets is not null)
                  {
                     foreach (var target in command.Targets)
                     {
                        if (target.Valid)
                        {
                           builder.Add(checked(offset + target.Offset),
                              target.Multiplier);
                        }
                     }
                  }
                  break;
            }
            builder.AddConst(offset,
               command.Node!.Value, command.Node.Overwrite);
         }
      }

      public void Emit(Builder builder)
      {
         EmitBeginBlock(builder);
         EmitCommands(builder);
         EmitEndBlock(builder);
      }
   }
}
