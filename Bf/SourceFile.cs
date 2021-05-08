using System;
using System.IO;
using Bf.Core;

namespace Bf
{
   class SourceFile : ISource
   {
      readonly string path;

      public SourceFile(string path) => this.path = path;

      public ReadOnlySpan<byte> GetBytes() => File.ReadAllBytes(path);

      public void Error(SyntaxError error) =>
         Console.Error.WriteLine(error.Message);
   }
}
