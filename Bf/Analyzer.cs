using System;

namespace Bf
{
   class Analyzer
   {
      public bool Parse(ReadOnlySpan<byte> source)
      {
         var scanner = new Scanner(source);
         while (scanner.MoveNext())
         {
            switch (scanner.Current)
            {
               case Token.Increment:
                  break;
               case Token.Decrement:
                  break;
               case Token.MoveRight:
                  break;
               case Token.MoveLeft:
                  break;
               case Token.Write:
                  break;
               case Token.Read:
                  break;
               case Token.BeginLoop:
                  break;
               case Token.EndLoop:
                  break;
               case Token.InvalidBracket:
                  while (scanner.MoveNext()) { }
                  break;
            }
         }
         return scanner.IsValid;
      }
   }
}
