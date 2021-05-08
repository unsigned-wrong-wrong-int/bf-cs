using System;
using System.Text;
using System.Linq;
using Xunit;
using Bf.Core;

namespace Bf.Tests
{
   public sealed class ScannerTests
   {
      [Fact]
      internal void AcceptsEmptyInput()
      {
         var scanner = new Scanner(Array.Empty<byte>());

         Assert.False(scanner.MoveNext());

         Assert.Null(scanner.Errors);
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

         Assert.Null(scanner.Errors);
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

         Assert.Null(scanner.Errors);
      }

      [Fact]
      internal void ChecksForClosingBracket()
      {
         var scanner = new Scanner(new[] { (byte)'[' });

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.BeginLoop, scanner.Current);

         Assert.False(scanner.MoveNext());

         Assert.NotNull(scanner.Errors);

         var error = scanner.Errors!.Dequeue().Message;
         Assert.Equal("Unmatched [ at 1:1", error);

         Assert.Empty(scanner.Errors);
      }

      [Fact]
      internal void ChecksForOpeningBracket()
      {
         var scanner = new Scanner(new[] { (byte)']' });

         Assert.True(scanner.MoveNext());
         Assert.Equal(Token.InvalidBracket, scanner.Current);

         Assert.False(scanner.MoveNext());

         Assert.NotNull(scanner.Errors);

         var error = scanner.Errors!.Dequeue().Message;
         Assert.Equal("Unmatched ] at 1:1", error);

         Assert.Empty(scanner.Errors);
      }

      static void AssertTokens(
         ReadOnlySpan<Token> expected, string[]? expectedErrors,
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

         if (expectedErrors is null)
         {
            Assert.Null(scanner.Errors);
         }
         else
         {
            Assert.NotNull(scanner.Errors);

            var errors = scanner.Errors!.Select(error => error.Message);
            Assert.Equal(expectedErrors, errors);
         }
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
            null,
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
            null,
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
            null,
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
            new[] {
               "Unmatched ] at 1:1",
               "Unmatched ] at 1:2",
               "Unmatched ] at 1:5",
               "Unmatched [ at 1:10",
               "Unmatched [ at 1:7",
               "Unmatched [ at 1:6",
            },
            "]][]][[[]["
         );
      }
   }
}
