using System;
using System.IO;
using Xunit;

namespace Bf.Tests
{
   public sealed class ReadByteTests : IDisposable
   {
      readonly TextReader consoleIn;

      public ReadByteTests()
      {
         consoleIn = Console.In;
      }

      public void Dispose()
      {
         Console.SetIn(consoleIn);
      }

      [Theory]
      [InlineData(255)]
      [InlineData(0)]
      internal void ReturnsEOF(byte eof)
      {
         Console.SetIn(new StringReader(""));
         var runtime = new Runtime()
         {
            EOF = eof
         };

         var result = runtime.Read();
         Assert.Equal(eof, result);
      }

      [Fact]
      internal void AllowsReadingAtEOF()
      {
         Console.SetIn(new StringReader(""));
         var runtime = new Runtime();

         var result = runtime.Read();
         Assert.Equal(runtime.EOF, result);

         result = runtime.Read();
         Assert.Equal(runtime.EOF, result);
      }

      [Theory]
      [InlineData("\n", "\r")]
      [InlineData("\n", "\n")]
      [InlineData("\n", "\r\n")]
      [InlineData("\r\n", "\r")]
      [InlineData("\r\n", "\n")]
      [InlineData("\r\n", "\r\n")]
      internal void NormalizesNewLine(string newline, string input)
      {
         Console.SetIn(new StringReader(input));
         var runtime = new Runtime()
         {
            NewLine = newline
         };

         var result = runtime.Read();
         foreach (var c in newline)
         {
            Assert.Equal((byte)c, result);
            result = runtime.Read();
         }
         Assert.Equal(runtime.EOF, result);
      }

      [Theory]
      [InlineData('A')]
      [InlineData(' ')]
      [InlineData('\t')]
      internal void ReadsAsciiChar(char c)
      {
         Console.SetIn(new StringReader(c + "\n"));
         var runtime = new Runtime();

         var result = runtime.Read();
         Assert.Equal((byte)c, result);

         result = runtime.Read();
         Assert.Equal((byte)runtime.NewLine[0], result);
      }

      static void AssertUTF8(string expectedText, string input)
      {
         Console.SetIn(new StringReader(input + "\n"));
         var runtime = new Runtime();

         ReadOnlySpan<byte> expected =
            System.Text.Encoding.UTF8.GetBytes(expectedText);

         var result = runtime.Read();
         foreach (var b in expected)
         {
            Assert.Equal(b, result);
            result = runtime.Read();
         }
         Assert.Equal((byte)runtime.NewLine[0], result);
      }

      [Theory]
      [InlineData('α')]
      [InlineData('→')]
      [InlineData('　')]
      internal void ReadsTextAsUTF8(char c)
      {
         var s = c.ToString();
         AssertUTF8(s, s);
      }

      [Theory]
      [InlineData("𝔸")]
      [InlineData("😃")]
      internal void HandlesSurrogatePair(string s)
      {
         AssertUTF8(s, s);
      }

      [Fact]
      internal void UsesReplacementChar()
      {
         AssertUTF8("�", "\ud800");
      }
   }
}
