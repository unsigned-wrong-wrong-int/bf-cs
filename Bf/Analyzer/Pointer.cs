using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bf.Analyzer
{
   class Pointer
   {
      public Pointer? Next { get; private set; }

      public Context Context { get; }

      readonly bool isInitial;
      public bool IsStartOfLoop { get; }
      public bool IsEndOfLoop { get; private set; }

      public int MaxOffset { get; private set; }
      public int MinOffset { get; private set; }
      int offset;
      public int LastOffset => offset;

      readonly Dictionary<int /* offset */, Cell> cells;

      readonly Queue<(int offset, Command)> commands;

      public CommandSequence GetCommands() => new(cells, commands);

      public Pointer(Context context, bool isStartOfLoop)
      {
         Next = null;
         Context = context;
         IsStartOfLoop = isStartOfLoop;
         isInitial = false;
         IsEndOfLoop = false;
         MaxOffset = MinOffset = offset = 0;
         cells = new();
         commands = new();
      }

      public Pointer() : this(new(false), isStartOfLoop: false)
      {
         isInitial = true;
      }

      Cell GetCell(int pos = 0)
      {
         checked
         {
            pos += offset;
         }
         if (pos > MaxOffset)
         {
            MaxOffset = pos;
         }
         else if (pos < MinOffset)
         {
            MinOffset = pos;
         }
         if (!cells.TryGetValue(pos, out var cell))
         {
            cell = new(isZero: isInitial || !IsStartOfLoop && pos == 0);
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
         Context.PerformsIO = true;
         commands.Enqueue((offset, GetCell().Write()));
      }

      public void Read()
      {
         Context.PerformsIO = true;
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
         loopStart = new(new(cell.IsNonZero), isStartOfLoop: true);
         return true;
      }

      void AppendLoop(Pointer start, Pointer end)
      {
         Next = start;
         end.IsEndOfLoop = true;
         end.Next = new(Context, isStartOfLoop: false);
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
         if (!Context.PerformsIO)
         {
            outer.commands.Enqueue((outer.offset,
               Command.InfiniteLoop(Context.IsConditional)));
            return Context.IsConditional;
         }
         Context.Repetition = Repetition.Infinite;
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool ToConditional(Pointer outer, Pointer loopEnd)
      {
         if (!Context.IsConditional)
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
         Context.Repetition = Repetition.Once;
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
         Context.Close(offset, outer.Context);
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
            if (Next is null && !Context.PerformsIO)
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
         if (IsStartOfLoop)
         {
            if (Context.IsConditional)
            {
               builder.BeginIf();
            }
            if (Context.Repetition != Repetition.Once)
            {
               switch (Context.Move)
               {
                  case PointerMove.Fixed:
                     builder.CheckUpperBound(MaxOffset);
                     builder.CheckLowerBound(MinOffset);
                     builder.BeginLoop();
                     return;
                  case PointerMove.Forward:
                     builder.CheckLowerBound(MinOffset);
                     builder.BeginLoop();
                     builder.CheckUpperBound(MaxOffset);
                     return;
                  case PointerMove.Backward:
                     builder.CheckUpperBound(MaxOffset);
                     builder.BeginLoop();
                     builder.CheckLowerBound(MinOffset);
                     return;
               }
               builder.BeginLoop();
            }
         }
         builder.CheckUpperBound(MaxOffset);
         builder.CheckLowerBound(MinOffset);
      }

      void EmitEndBlock(Builder builder)
      {
         builder.Move(offset);
         if (!IsEndOfLoop)
         {
            return;
         }
         if (Context.Repetition != Repetition.Once)
         {
            builder.EndLoop(Context.Repetition == Repetition.Ordinary);
         }
         if (Context.IsConditional)
         {
            builder.EndIf();
         }
      }

      void EmitCommands(Builder builder)
      {
         foreach (var (offset, command) in GetCommands())
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
                  continue;
               case CommandType.Load:
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
