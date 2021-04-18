using System.Collections.Generic;

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

      public Cell GetCell(int pos = 0)
      {
         pos += offset;
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

      public void MoveRight()
      {
         if (++offset > maxOffset)
         {
            maxOffset = offset;
         }
      }

      public void MoveLeft()
      {
         if (--offset < minOffset)
         {
            minOffset = offset;
         }
      }
   }
}
