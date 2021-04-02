using System.Collections.Generic;

namespace Bf.Analyzer
{
   class Cell
   {
      List<Node>? previous;

      public Node Current { get; private set; }

      public Cell()
      {
         previous = null;
         Current = new();
      }

      public void Add(byte value)
      {
         Current.Value += value;
      }

      public void AddTerm(byte multipiler, Node node)
      {
         Current.AddTerm(new(multipiler, node));
      }

      public Node Load(bool clear = false, byte shiftRight = 0)
      {
         var current = Current;
         if (previous is null)
         {
            previous = new();
         }
         previous.Add(current);
         Current = new()
         {
            Overwrite = clear,
            ShiftRight = shiftRight,
         };
         return current;
      }

      public bool IsConst => Current.Overwrite && Current.Terms is null;

      public bool IsZero => IsConst && Current.Value == 0;

      public bool IsNonZero => IsConst && Current.Value != 0;
   }
}
