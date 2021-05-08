namespace Bf.Analyzer
{
   class Cell
   {
      readonly Node head;
      Node current;

      public Command Initializer { get; }

      public Cell(bool isZero)
      {
         head = current = new(null, overwrite: isZero);
         Initializer = Command.Initialize(head);
      }

      public void Increment() => ++current.Value;

      public void Decrement() => --current.Value;

      public bool IsZero => current.IsConst && current.Value == 0;

      public bool IsNonZero => current.IsConst && current.Value != 0;

      public bool IsNoop => current.Previous is null &&
         !current.Overwrite && current.Value == 0 && !current.IsDependent;

      public Command Write()
      {
         if (current.IsConst)
         {
            return Command.Write(current.Value);
         }
         current = new(current, overwrite: false);
         return Command.Write(current);
      }

      public Command Read()
      {
         current.Clear(overwrite: false);
         current = new(current, overwrite: false);
         return Command.Read(current);
      }

      public void Merge(Cell cell) => current = cell.head.Prepend(current);

      public byte? GetDivisor()
      {
         if (current.Previous is not null || current.Overwrite ||
            current.IsDependent)
         {
            return null;
         }
         return (byte)-current.Value;
      }

      public Step? GetStep(int offset)
      {
         if (current.Previous is { } prev)
         {
            if (prev.Previous is not null ||
               prev.Overwrite || prev.IsDependent ||
               current.Value != 0 || current.IsDependent)
            {
               return null;
            }
            return new(prev, offset, isConsumed: true);
         }
         else
         {
            if (current.Overwrite)
            {
               return null;
            }
            return new(current, offset, isConsumed: false);
         }
      }

      public Command? Load(byte divisor, out byte multiplier)
      {
         multiplier = divisor.Reciprocal(out var shiftRight);
         if (current.IsConst)
         {
            if ((current.Value & ~(-1 << shiftRight)) != 0)
            {
               return Command.InfiniteLoop(isConditional: false);
            }
            multiplier *= (byte)(current.Value >> shiftRight);
            current.Clear(overwrite: true);
            return null;
         }
         current = new(current, overwrite: true);
         return Command.Load(current, shiftRight);
      }

      public void Add(Step step, byte multiplier)
      {
         multiplier *= step.Node.Value;
         if (multiplier != 0)
         {
            current.Value += multiplier;
         }
         if (step.IsConsumedInLoop)
         {
            current = new(current, overwrite: true);
         }
      }

      public void Add(Step step, byte multiplier, Command command)
      {
         multiplier *= step.Node.Value;
         if (multiplier != 0)
         {
            command.AddTarget(current, step.Offset, multiplier);
         }
         if (step.IsConsumedInLoop)
         {
            current = new(current, overwrite: true);
         }
      }
   }

   readonly struct Step
   {
      // [- > +++[->++<] > ++ < < ]
      //      ^^^          ^^
      //       |          Step { IsConsumedInLoop = false }
      //      Step { IsConsumedInLoop = true }

      public Node Node { get; }
      public int Offset { get; }
      public bool IsConsumedInLoop { get; }

      public Step(Node node, int offset, bool isConsumed)
      {
         Node = node;
         Offset = offset;
         IsConsumedInLoop = isConsumed;
      }
   }
}
