using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Bf.Core;

namespace Bf
{
   class Runtime : IRuntime
   {
      public int Cells { get; set; } = 0x8000;

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

      public byte[] Start() => new byte[Cells];

      public void End() => Flush();

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
         // 0xxxxxxx -> 0x ; ASCII char
         // 10xxxxxx -> 10 ; continuation byte
         // 110xxxxx -> 11 ; first byte
         // 1110xxxx -> 11 ; first byte
         // 11110xxx -> 11 ; first byte
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
               Flush();
               Console.Write((char)value);
               break;
         }
      }

      public void Flush()
      {
         if (outputCount > 0)
         {
            Console.Write(utf8.GetString(output, 0, outputCount));
            outputCount = 0;
         }
      }

      public void Abort(RuntimeError error)
      {
         Flush();
         Console.Error.WriteLine($"Runtime Error: {error.GetMessage()}");
      }
   }
}
