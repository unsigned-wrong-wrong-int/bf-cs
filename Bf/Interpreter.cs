using System;
using System.IO;

namespace Bf
{
   enum EOF
   {
      Zero,
      MinusOne,
      Unchanged,
   }

   class Interpreter
   {
      public TextReader Stdin { get; }

      public TextWriter Stdout { get; }

      public TextWriter Stderr { get; }

      int size = 0x8000;
      public int Size {
         get => size;
         set => size = Math.Clamp(value, 1, 0x4000_0000);
      }

      public EOF EOF { get; set; }

      public Interpreter(TextReader stdin, TextWriter stdout, TextWriter stderr)
      {
         Stdin = stdin;
         Stdout = stdout;
         Stderr = stderr;
      }

      public Interpreter() : this(Console.In, Console.Out, Console.Error) { }
   }
}
