using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Bf.Analyzer;

namespace Bf.Tests
{
   public sealed class DivisionTests
   {
      internal static IEnumerable<object[]> NonZeroBytes() =>
         Enumerable.Range(1, 255).Select(n => new object[] { n });

      [Theory]
      [MemberData(nameof(NonZeroBytes))]
      internal void ReturnsCorrectReciprocal(byte value)
      {
         var reciprocal = value.Reciprocal(out var shiftRight);
         var product = (byte)(reciprocal * (value >> shiftRight));
         Assert.Equal(1, product);
      }

      static byte UnoptimizedDivision(byte a, byte b)
      {
         if (b == 0)
         {
            throw new ArgumentOutOfRangeException(nameof(b));
         }
         byte result = 0;
         for (; a != 0; ++result)
         {
            a -= b;
         }
         return result;
      }

      [Theory]
      [InlineData(1, 1)]
      [InlineData(1, 3)]
      [InlineData(1, 5)]
      [InlineData(1, 7)]
      [InlineData(1, 253)]
      [InlineData(1, 255)]
      [InlineData(0, 3)]
      [InlineData(2, 3)]
      [InlineData(3, 3)]
      [InlineData(4, 3)]
      [InlineData(253, 3)]
      [InlineData(254, 3)]
      [InlineData(255, 3)]
      [InlineData(0, 2)]
      [InlineData(2, 2)]
      [InlineData(6, 2)]
      [InlineData(254, 2)]
      [InlineData(128, 64)]
      [InlineData(192, 64)]
      internal void GivesSameResultAsLoop(byte a, byte b)
      {
         byte reciprocal = b.Reciprocal(out var shiftRight);
         var result = (byte)(reciprocal * (a >> shiftRight));

         var unoptimized = UnoptimizedDivision(a, b);

         Assert.Equal(unoptimized, result);
      }
   }
}
