using System;
using System.IO;
using System.Text;
using Xunit;

namespace Bf.Tests
{
   public sealed class WriteByteTests : IDisposable
   {
      readonly TextWriter consoleOut;
      readonly StringWriter stdout;

      public WriteByteTests()
      {
         consoleOut = Console.Out;
         stdout = new();
         Console.SetOut(stdout);
      }

      public void Dispose()
      {
         Console.SetOut(consoleOut);
      }

      [Theory]
      [InlineData('A')]
      [InlineData(' ')]
      [InlineData('\t')]
      internal void WritesAsciiChar(char c)
      {
         var runtime = new Runtime();

         runtime.Write((byte)c);

         runtime.Flush();

         var output = stdout.ToString();
         Assert.Equal(c.ToString(), output);
      }

      void AssertUTF8(string expected, string inputText)
      {
         var runtime = new Runtime();

         ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes(inputText);

         foreach (var b in input)
         {
            runtime.Write(b);
         }

         runtime.Flush();

         var output = stdout.ToString();
         Assert.Equal(expected, output);
      }

      [Theory]
      [InlineData('α')]
      [InlineData('→')]
      [InlineData('　')]
      internal void WritesBytesAsUTF8(char c)
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

      [Fact]
      internal void WritesCharSequence()
      {
         AssertUTF8("Aα𝔸a", "Aα𝔸a");
      }
   }
}
