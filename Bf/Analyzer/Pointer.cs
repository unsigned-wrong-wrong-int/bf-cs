using System.Collections.Generic;

namespace Bf.Analyzer
{
   class Pointer
   {
      int basePos;
      int maxOffset;
      int minOffset;
      int offset;

      readonly Dictionary<int /* offset */, Cell> cells;

      readonly List<Command> commands;

      public Pointer(int pos)
      {
         basePos = pos;
         maxOffset = minOffset = offset = 0;
         cells = new();
         commands = new();
      }

      public Cell this[int pos]
      {
         get
         {
            if (!cells.TryGetValue(pos, out var cell))
            {
               cell = new();
               cells.Add(pos, cell);
            }
            return cell;
         }
      }

      public Cell Cell => this[offset];

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
