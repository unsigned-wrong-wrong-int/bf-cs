using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Bf
{
   class Runtime
   {
      public byte EOF { get; set; } = 255;

      public string NewLine { get; set; } = "\n";

      readonly UTF8Encoding utf8;

      public Runtime()
      {
         utf8 = new(false);
         input = Array.Empty<byte>().AsEnumerable().GetEnumerator();
         output = new byte[4];
         outputCount = 0;
      }

      IEnumerator<byte> input;

      public byte Read()
      {
         if (!input.MoveNext())
         {
            if (Console.ReadLine() is not { } line)
            {
               return EOF;
            }
            line += NewLine;
            input = utf8.GetBytes(line).AsEnumerable().GetEnumerator();
            _ = input.MoveNext();
         }
         return input.Current;
      }

      readonly byte[] output;
      int outputCount;

      public void Write(byte value)
      {
         switch (value >> 6)
         {
            case 0b10:
               output[outputCount++] = value;
               if (outputCount == 4)
               {
                  Flush();
               }
               break;
            case 0b11:
               Flush();
               output[0] = value;
               outputCount = 1;
               break;
            default:
               Console.Write((char)value);
               break;
         }
      }

      public void Flush()
      {
         if (outputCount > 0)
         {
            Console.Write(utf8.GetString(output, 0, outputCount));
         }
      }

      public static void Error(string message) =>
         Console.Error.WriteLine($"Runtime Error: {message}");
   }
}
