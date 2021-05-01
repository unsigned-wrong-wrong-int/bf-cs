using System.Collections.Generic;

namespace Bf.Analyzer
{
   class Analyzer
   {
      readonly Stack<(Pointer start, Pointer current)> loopStack;
      Pointer start;
      Pointer current;

      public Analyzer()
      {
         loopStack = new();
         current = start = new();
      }

      public void Increment() => current.Increment();
      public void Decrement() => current.Decrement();

      public void MoveRight() => current.MoveRight();
      public void MoveLeft() => current.MoveLeft();

      public void Write() => current.Write();
      public void Read() => current.Read();

      public bool BeginLoop()
      {
         if (current.BeginLoop(out var loopStart))
         {
            loopStack.Push((start, current));
            current = start = loopStart;
            return true;
         }
         return false;
      }

      public bool EndLoop()
      {
         var (outerStart, outer) = loopStack.Pop();
         if (outer.EndLoop(start, current))
         {
            start = outerStart;
            current = outer;
            return true;
         }
         return false;
      }
   }
}
