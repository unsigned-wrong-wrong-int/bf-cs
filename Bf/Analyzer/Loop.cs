namespace Bf.Analyzer
{
   // [ - > +++[->++<] > ++ < < ]
   //   ^   ^^^          ^^
   //   |    |          LoopLocal
   //   |   LoopLocal { IsConsumedInLoop = true }
   // LoopCounter

   readonly struct LoopCounter
   {
      public byte Value { get; }

      public LoopCounter(byte value)
      {
         Value = value;
      }
   }

   readonly struct LoopLocal
   {
      public Node Node { get; }
      public bool IsConsumedInLoop { get; }
      public bool IsNonZero { get; }
      public bool IsNoop { get; }

      public LoopLocal(Node node, bool isConsumed)
      {
         Node = node;
         IsConsumedInLoop = isConsumed;
         IsNonZero = node.Value != 0;
         IsNoop = !IsNonZero && node.Terms is null;
      }
   }
}
