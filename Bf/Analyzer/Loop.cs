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

   readonly struct LoopStep
   {
      public byte Multiplier { get; }
      public Node? Node { get; }

      LoopStep(byte multiplier, Node? node)
      {
         Multiplier = multiplier;
         Node = node;
      }

      public static LoopStep? Divide(Node node, byte divisor)
      {
         var reciprocal = divisor.Reciprocal(out var shiftRight);
         if (node.IsConst)
         {
            var value = node.Value;
            if ((value & ~(-1 << shiftRight)) != 0)
            {
               // e.g. value: 3, shiftRight: 1
               // +++[--]
               return null;
            }
            return new((byte)((value >> shiftRight) * reciprocal), null);
         }
         node.ShiftRight = shiftRight;
         return new(reciprocal, node);
      }

      public void AddProduct(Node node, byte value)
      {
         if (Node is null)
         {
            node.Value += (byte)(Multiplier * value);
         }
         else
         {
            node.AddTerm(new((byte)(Multiplier * value), Node));
         }
      }
   }
}
