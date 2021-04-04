namespace Bf.Analyzer
{
   class Cell
   {
      readonly Node head;
      Node current;

      public Cell()
      {
         head = current = new();
      }

      public void Increment() => ++current.Value;

      public void Decrement() => --current.Value;

      public bool IsConst => current.Overwrite && current.Terms is null;

      public bool IsZero => IsConst && current.Value == 0;

      public bool IsNonZero => IsConst && current.Value != 0;
   }
}
