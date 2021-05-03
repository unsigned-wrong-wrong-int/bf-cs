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
      Pointer? next;
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
         next = null;
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
         next = start;
         end.isEndOfLoop = true;
         end.next = new(context, PointerState.AfterLoop);
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
         context.End = ExitBlock.Never;
         outer.AppendLoop(this, loopEnd);
         return context.Start == EnterBlock.IfNonZero;
      }

      bool ToConditional(Pointer outer, Pointer loopEnd)
      {
         if (context.Start == EnterBlock.Always)
         {
            outer.EnqueueCommands(commands);
            foreach (var (pos, cell) in cells)
            {
               outer.GetCell(pos).Merge(cell);
            }
            if (next is not null)
            {
               outer.AppendLoop(next, loopEnd);
            }
            return true;
         }
         context.End = ExitBlock.Always;
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
            if (command.Node is null)
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
            if (next is null && !context.PerformsIO)
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
   }
}
