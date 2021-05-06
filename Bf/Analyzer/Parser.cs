using System;
using System.Collections.Generic;

namespace Bf.Analyzer
{
   class Parser
   {
      readonly Stack<(Pointer start, Pointer current)> loopStack;
      Pointer start;
      Pointer current;

      public Parser()
      {
         loopStack = new();
         current = start = new();
      }

      public Pointer? Parse(ReadOnlySpan<byte> source)
      {
         Scanner scanner = new(source);
         while (scanner.MoveNext())
         {
            switch (scanner.Current)
            {
               case Token.Increment:
                  current.Increment();
                  break;
               case Token.Decrement:
                  current.Decrement();
                  break;
               case Token.MoveRight:
                  current.MoveRight();
                  break;
               case Token.MoveLeft:
                  current.MoveLeft();
                  break;
               case Token.Write:
                  current.Write();
                  break;
               case Token.Read:
                  current.Read();
                  break;
               case Token.BeginLoop:
                  if (current.BeginLoop(out var loopStart))
                  {
                     loopStack.Push((start, current));
                     current = start = loopStart;
                     break;
                  }
                  _ = scanner.SkipCurrentLoop();
                  break;
               case Token.EndLoop:
                  var (outerStart, outer) = loopStack.Pop();
                  if (outer.EndLoop(start, current))
                  {
                     (start, current) = (outerStart, outer);
                     break;
                  }
                  if (scanner.SkipCurrentLoop())
                  {
                     (start, current) = loopStack.Pop();
                  }
                  break;
               case Token.InvalidBracket:
                  scanner.SkipRest();
                  break;
            }
         }
         return scanner.IsValid ? start : null;
      }
   }
}
