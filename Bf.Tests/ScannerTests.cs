using System;
using System.IO;
using Xunit;

namespace Bf.Tests
{
   public sealed class ScannerTests : IDisposable
   {
      readonly TextWriter consoleErr;
      readonly StringWriter stderr;

      public ScannerTests()
      {
         consoleErr = Console.Error;
         stderr = new();
         Console.SetError(stderr);
      }

      public void Dispose()
      {
         Console.SetError(consoleErr);
      }

      [Fact]
      internal void AcceptsEmptyInput()
      {
         var scanner = new Scanner(Array.Empty<byte>());

         Assert.False(scanner.MoveNext());

         var error = stderr.ToString();
         Assert.Empty(error);
      }

      [Theory]
      [InlineData((byte)'+', Token.Increment)]
      [InlineData((byte)'-', Token.Decrement)]
      [InlineData((byte)'>', Token.MoveRight)]
      [InlineData((byte)'<', Token.MoveLeft)]
      [InlineData((byte)'.', Token.Write)]
      [InlineData((byte)',', Token.Read)]
      internal void ScansSingleCharCommands(byte command, Token token)
      {
         var scanner = new Scanner(new[] { command });

         Assert.True(scanner.MoveNext());
         Assert.Equal(token, scanner.Current);

         Assert.False(scanner.MoveNext());

         var error = stderr.ToString();
         Assert.Empty(error);
      }
   }
}
