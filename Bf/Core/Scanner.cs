using System;
using System.Collections.Generic;

namespace Bf.Core
{
   enum Token : int
   {
      Increment = '+',
      Decrement = '-',
      MoveRight = '>',
      MoveLeft = '<',
      Write = '.',
      Read = ',',
      BeginLoop = '[',
      EndLoop = ']',
      InvalidBracket = -1,
   }

   ref struct Scanner
   {
      ReadOnlySpan<byte>.Enumerator inner;

      Token current;

      int line;
      int column;
      readonly Stack<(int line, int column)> loopStarts;

      public Queue<SyntaxError>? Errors { get; private set; }

      public Scanner(ReadOnlySpan<byte> source)
      {
         inner = source.GetEnumerator();
         current = default;
         line = column = 1;
         loopStarts = new();
         Errors = null;
      }

      public bool MoveNext()
      {
         if (!inner.MoveNext())
         {
            foreach (var (line, column) in loopStarts)
            {
               Error('[', line, column);
            }
            loopStarts.Clear();
            return false;
         }
         current = (Token)inner.Current;
         switch (current)
         {
            case Token.BeginLoop:
               loopStarts.Push((line, column));
               goto default;
            case Token.EndLoop:
               if (!loopStarts.TryPop(out _))
               {
                  Error(']', line, column);
                  current = Token.InvalidBracket;
               }
               goto default;
            default:
               ++column;
               break;
            case (Token)'\n':
               ++line;
               column = 1;
               break;
         }
         return true;
      }

      public Token Current => current;

      public bool SkipCurrentLoop()
      {
         var level = 1;
         while (MoveNext())
         {
            switch (current)
            {
               case Token.BeginLoop:
                  ++level;
                  break;
               case Token.EndLoop:
                  if (--level == 0)
                  {
                     return true;
                  }
                  break;
            }
         }
         return false;
      }

      public void SkipRest()
      {
         while (MoveNext()) { }
      }

      void Error(char @char, int line, int column)
      {
         if (Errors is null)
         {
            Errors = new();
         }
         Errors.Enqueue(new(@char, line, column));
      }
   }
}
