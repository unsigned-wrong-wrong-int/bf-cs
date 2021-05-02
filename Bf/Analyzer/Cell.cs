namespace Bf.Analyzer
{
   class Cell
   {
      public Node Head { get; }
      Node current;

      public Cell(bool isZero)
      {
         Head = current = new(null, overwrite: isZero);
      }

      public void Increment() => ++current.Value;

      public void Decrement() => --current.Value;

      public bool IsZero => current.IsConst && current.Value == 0;

      public bool IsNonZero => current.IsConst && current.Value != 0;

      public bool IsNoop => current.Previous is null &&
         !current.Overwrite && current.Value != 0 && !current.IsDependent;

      public Command Write()
      {
         if (current.IsConst)
         {
            return Command.Write(current.Value);
         }
         var node = current;
         current = new(current, overwrite: false);
         return Command.Write(node);
      }

      public Command Read()
      {
         var node = current;
         current.Clear(overwrite: false);
         current = new(current, overwrite: false);
         return Command.Read(node);
      }

      public void Merge(Cell cell)
      {
         var next = cell.Head;
         next.Prepend(current);
         current = next;
      }

      public byte? GetDivisor()
      {
         if (current.Previous is not null || current.Overwrite ||
            current.IsDependent)
         {
            return null;
         }
         return current.Value;
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
               return Command.Load(null, shiftRight);
            }
            multiplier *= (byte)(current.Value >> shiftRight);
            return null;
         }
         current = new(current, overwrite: true);
         return Command.Load(current, shiftRight);
      }

      public void Add(Step step, byte multiplier, Command? command)
      {
         var node = step.Node;
         if (node.Value != 0)
         {
            if (command is null)
            {
               current.Value += (byte)(multiplier * node.Value);
            }
            else
            {
               command.AddTarget(current, step.Offset, multiplier);
            }
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
