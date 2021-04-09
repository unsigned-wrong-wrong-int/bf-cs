namespace Bf.Analyzer
{
   class Cell
   {
      readonly Node head;
      Node current;

      public Cell()
      {
         head = current = new(null, overwrite: false);
      }

      public void Increment() => ++current.Value;

      public void Decrement() => --current.Value;

      public bool IsZero => current.IsConst && current.Value == 0;

      public bool IsNonZero => current.IsConst && current.Value != 0;

      public LoopCounter? AsLoopCounter()
      {
         if (current.Previous is not null || current.Overwrite ||
            current.Terms is not null)
         {
            return null;
         }
         return new(current.Value);
      }

      public LoopLocal? AsLoopLocal()
      {
         if (current.Previous is {} prev)
         {
            if (prev.Previous is not null || prev.Overwrite ||
               current.Terms is not null || current.Value != 0)
            {
               return null;
            }
            return new(prev, isConsumed: true);
         }
         else
         {
            if (current.Overwrite)
            {
               return null;
            }
            return new(current, isConsumed: false);
         }
      }

      public void Merge(Cell cell)
      {
         var next = cell.head;
         next.Prepend(current);
         current = next;
      }

      public void Merge(LoopCounter counter)
      {
         // [-]   { *p = 0; }
         _ = current.DivideBy(counter.Value);
         current.SetZero();
         if (current.ShiftRight != 0)
         {
            // [--]
            // { assert((*p & 1) == 0, "infinite loop"); *p = 0; }
            current = new(current, overwrite: true);
         }
      }

      public void Merge(LoopCounter counter, out Term step)
      {
         step = current.DivideBy(counter.Value);
         current = new(current, overwrite: true);
      }

      public void Merge(LoopLocal local, Term step)
      {
         var node = local.Node;
         if (local.IsNonZero)
         {
            current.AddTerm(step.MultiplyBy(node.Value));
            node.Value = 0;
         }
         node.Prepend(current);
         current =
            local.IsConsumedInLoop ? new(node, overwrite: true) : node;
      }
   }
}
