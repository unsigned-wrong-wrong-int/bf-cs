using System;
using System.IO;
using System.Text;
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
         stderr.NewLine = "\n";
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

         Assert.True(scanner.IsValid);
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

         Assert.True(scanner.IsValid);
         var error = stderr.ToString();
         Assert.Empty(error);
      }

      [Fact]
      internal void AcceptsPairedBrackets()
      {
         var scanner = new Scanner(new[] { (byte)'[', (byte)']', });

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.BeginLoop, scanner.Current);

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.EndLoop, scanner.Current);

         Assert.False(scanner.MoveNext());

         Assert.True(scanner.IsValid);
         var error = stderr.ToString();
         Assert.Empty(error);
      }

      [Fact]
      internal void ChecksForClosingBracket()
      {
         var scanner = new Scanner(new[] { (byte)'[' });

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.BeginLoop, scanner.Current);

         Assert.False(scanner.MoveNext());

         Assert.False(scanner.IsValid);
         var error = stderr.ToString();
         Assert.Equal("Syntax Error: Unmatched [ at 1:1\n", error);
      }

      [Fact]
      internal void ChecksForOpeningBracket()
      {
         var scanner = new Scanner(new[] { (byte)']' });

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.InvalidBracket, scanner.Current);

         Assert.False(scanner.MoveNext());

         Assert.False(scanner.IsValid);
         var error = stderr.ToString();
         Assert.Equal("Syntax Error: Unmatched ] at 1:1\n", error);
      }

      static void AssertTokens(ReadOnlySpan<Token> expected, bool isValid,
         string input)
      {
         var bytes = Encoding.UTF8.GetBytes(input);
         var scanner = new Scanner(bytes);

         foreach (var token in expected)
         {
            Assert.True(scanner.MoveNext());
            Assert.Equal(token, scanner.Current);
         }

         Assert.False(scanner.MoveNext());

         Assert.Equal(isValid, scanner.IsValid);
      }

      [Fact]
      internal void HandlesNestedBrackets()
      {
         AssertTokens(
            new[] {
               Token.BeginLoop,
               Token.BeginLoop,
               Token.EndLoop,
               Token.EndLoop,
            },
            true,
            "[[]]"
         );

         AssertTokens(
            new[] {
               Token.BeginLoop,
               Token.BeginLoop,
               Token.BeginLoop,
               Token.BeginLoop,
               Token.EndLoop,
               Token.EndLoop,
               Token.EndLoop,
               Token.EndLoop,
            },
            true,
            "[[[[]]]]"
         );

         AssertTokens(
            new[] {
               Token.BeginLoop,
               Token.BeginLoop,
               Token.EndLoop,
               Token.BeginLoop,
               Token.BeginLoop,
               Token.EndLoop,
               Token.EndLoop,
               Token.EndLoop,
            },
            true,
            "[[][[]]]"
         );
      }

      [Fact]
      internal void DetectsAllUnbalancedBrackets()
      {
         AssertTokens(
            new[] {
               Token.InvalidBracket,
               Token.InvalidBracket,
               Token.BeginLoop,
               Token.EndLoop,
               Token.InvalidBracket,
               Token.BeginLoop,
               Token.BeginLoop,
               Token.BeginLoop,
               Token.EndLoop,
               Token.BeginLoop,
            },
            false,
            "]][]][[[]["
         );

         var error = stderr.ToString();
         Assert.Equal(
            @"Syntax Error: Unmatched ] at 1:1
Syntax Error: Unmatched ] at 1:2
Syntax Error: Unmatched ] at 1:5
Syntax Error: Unmatched [ at 1:10
Syntax Error: Unmatched [ at 1:7
Syntax Error: Unmatched [ at 1:6
",
            error
         );
      }
   }
}
