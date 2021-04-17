using System.Collections.Generic;

namespace Bf.Analyzer
{
   class Pointer
   {
      Pointer? next;
      readonly Context context;

      int maxOffset;
      int minOffset;
      int offset;

      readonly Dictionary<int /* offset */, Cell> cells;

      public Pointer(Context context)
      {
         next = null;
         this.context = context;
         maxOffset = minOffset = offset = 0;
         cells = new();
      }

      public Cell GetCell(int pos = 0)
      {
         pos += offset;
         if (!cells.TryGetValue(pos, out var cell))
         {
            cell = new();
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
