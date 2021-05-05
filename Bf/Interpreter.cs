using System;

namespace Bf
{
   class Interpreter
   {
      readonly Analyzer.Analyzer analyzer;

      public Interpreter()
      {
         analyzer = new();
      }

      public bool Parse(ReadOnlySpan<byte> source)
      {
         var scanner = new Scanner(source);
         while (scanner.MoveNext())
         {
            switch (scanner.Current)
            {
               case Token.Increment:
                  analyzer.Increment();
                  break;
               case Token.Decrement:
                  analyzer.Decrement();
                  break;
               case Token.MoveRight:
                  analyzer.MoveRight();
                  break;
               case Token.MoveLeft:
                  analyzer.MoveLeft();
                  break;
               case Token.Write:
                  analyzer.Write();
                  break;
               case Token.Read:
                  analyzer.Read();
                  break;
               case Token.BeginLoop:
                  if (!analyzer.BeginLoop())
                  {
                     scanner.SkipCurrentLoop();
                  }
                  break;
               case Token.EndLoop:
                  if (!analyzer.EndLoop())
                  {
                     scanner.SkipCurrentLoop();
                  }
                  break;
               case Token.InvalidBracket:
                  scanner.SkipRest();
                  break;
            }
         }
         return scanner.IsValid;
      }
   }
}
