using System;

namespace Bf.Core
{
   interface ISource
   {
      ReadOnlySpan<byte> GetBytes();

      void Error(SyntaxError error);
   }
}
