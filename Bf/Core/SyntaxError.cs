namespace Bf.Core
{
   readonly struct SyntaxError
   {
      public char Char { get; }
      public int Line { get; }
      public int Column { get; }

      public SyntaxError(char @char, int line, int column)
      {
         Char = @char;
         Line = line;
         Column = column;
      }

      public string Message => $"Unmatched {Char} at {Line}:{Column}";
   }
}
