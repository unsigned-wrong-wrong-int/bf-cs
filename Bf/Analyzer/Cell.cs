using System.Diagnostics.CodeAnalysis;

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

      public bool TryMerge(LoopCounter counter)
      {
         if (LoopStep.Divide(current, counter.Value) is null)
         {
            return false;
         }
         if (current.ShiftRight == 0)
         {
            // [-]   { *p = 0; }
            current.SetZero();
         }
         else
         {
            // [--]
            // { assert((*p & 1) == 0, "infinite loop"); *p = 0; }
            current = new(current, overwrite: true);
         }
         return true;
      }

      public bool TryMerge(LoopCounter counter,
         [NotNullWhen(true)] out LoopStep? step)
      {
         step = LoopStep.Divide(current, counter.Value);
         if (step is null)
         {
            return false;
         }
         current = new(current, overwrite: true);
         return true;
      }

      public void Merge(LoopLocal local, LoopStep step)
      {
         var node = local.Node;
         if (local.IsNonZero)
         {
            step.AddProduct(current, node.Value);
            node.Value = 0;
         }
         node.Prepend(current);
         current =
            local.IsConsumedInLoop ? new(node, overwrite: true) : node;
      }
   }
}
