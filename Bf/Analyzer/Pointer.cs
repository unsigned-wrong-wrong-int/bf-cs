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

      public Pointer(Context context, PointerState state)
      {
         next = null;
         this.context = context;
         this.state = state;
         isEndOfLoop = false;
         maxOffset = minOffset = offset = 0;
         cells = new();
      }

      public Pointer() : this(new(false), PointerState.Initial) { }

      void AppendLoop(Pointer start, Pointer end)
      {
         next = start;
         end.isEndOfLoop = true;
         end.next = new(context, PointerState.AfterLoop);
      }

      public Cell GetCell(int pos = 0)
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

      public Cell GetCellForIO()
      {
         context.PerformsIO = true;
         return GetCell();
      }

      public void MoveRight() => _ = checked(++offset);

      public void MoveLeft() => _ = checked(--offset);

      public bool BeginLoop([NotNullWhen(true)] out Pointer? loopStart)
      {
         if (!cells.TryGetValue(offset, out var cell))
         {
            loopStart = new(new(offset == 0), PointerState.StartOfLoop);
            return true;
         }
         if (cell.IsZero)
         {
            loopStart = null;
            return false;
         }
         loopStart = new(new(cell.IsNonZero), PointerState.StartOfLoop);
         return true;
      }

      bool ToInfiniteLoop(Pointer outer, Pointer loopEnd)
      {
         context.End = CellState.NonZero;
         outer.AppendLoop(this, loopEnd);
         return context.Start == CellState.Any;
      }

      bool ToConditional(Pointer outer, Pointer loopEnd)
      {
         if (context.Start == CellState.NonZero)
         {
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
         context.End = CellState.Zero;
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool ToMultipliation(Pointer outer, Cell last, Pointer loopEnd)
      {
         if (last.AsLoopCounter() is not { } counter)
         {
            outer.AppendLoop(this, loopEnd);
            return true;
         }

         // `cells[0]` is identical to `last`
         _ = cells.Remove(0);
         List<(int pos, LoopLocal local)> locals = new();
         foreach (var (pos, cell) in cells)
         {
            if (cell.AsLoopLocal() is not { } local)
            {
               goto leaveUnoptimized;
            }
            if (!local.IsNoop)
            {
               locals.Add((pos, local));
            }
         }

         if (locals.Count == 0)
         {
            return outer.GetCell().TryMerge(counter);
         }

         if (!outer.GetCell().TryMerge(counter, out var step))
         {
            return false;
         }
         foreach (var (pos, local) in locals)
         {
            outer.GetCell(pos).Merge(local, step.Value);
         }
         return true;

      leaveUnoptimized:
         // restore the removed cell
         cells.Add(0, last);
         outer.AppendLoop(this, loopEnd);
         return true;
      }

      bool OptimizeLoop(Pointer outer, Pointer loopEnd)
      {
         context.Close(offset);
         outer.context.Include(context);
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
