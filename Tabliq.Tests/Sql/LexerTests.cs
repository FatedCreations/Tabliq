using Xunit;
using Tabliq.Sql.Lexing;
using Tabliq.Sql.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Tabliq.Tests.Sql.TokenBuilder;
using System.Diagnostics;
using System.Linq;
using System;
namespace Tabliq.Tests.Sql;

public class LexerTests
{
    [Fact]
    public void Extract()
    {
        var lexer = new Lexer("""
            SELECT extract(quarter from date)
            FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            "extract",
            "(",
            "quarter",
            "from",
            "date",
            ")",
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }
    [Fact]
    public void Comment()
    {
        var lexer = new Lexer("""
            SELECT [Id] -- remaining comment
            FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            Identifier("Id", "Id"),
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }
    [Fact]
    public void LexSimpleSelect()
    {
        var lexer = new Lexer("SELECT Id FROM Users");
        var tokens = lexer.LexAll();
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.SelectKeyword);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.IdentifierToken && t.Text == "Id");
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.FromKeyword);
    }

    [Fact]
    public void Cte()
    {
        var lexer = new Lexer("""
            WITH cte AS (
                SELECT Id, Name
                FROM Users
                WHERE IsActive = 1
            )
            SELECT * FROM cte
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "WITH",
            "cte",
            "AS",
            "(",
            "SELECT",
            "Id",
            ",",
            "Name",
            "FROM",
            "Users",
            "WHERE",
            "IsActive",
            "=",
            1,
            ")",
            "SELECT",
            "*",
            "FROM",
            "cte",
            EOF()
            ), Tokens(tokens));
    }

    [Fact]
    public void DoubleQuotedidnetifier()
    {
        var lexer = new Lexer("""
            SELECT "Id" FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            Identifier("Id", "Id"),
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }

    [Fact]
    public void SquareBracketedIdentifier()
    {
        var lexer = new Lexer("""
            SELECT [Id] FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            Identifier("Id", "Id"),
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }

    [Fact]
    public void MultiPartIdentifiers()
    {
        var lexer = new Lexer("""
            SELECT [Id].foo FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            Identifier("Id", "Id"),
            ".",
            Identifier("foo"),
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }

    [Fact]
    public void BackTickIdnetifier()
    {
        var lexer = new Lexer("""
            SELECT `Id` FROM Users
            """);

        var tokens = lexer.LexAll();
        Assert.Equal(Tokens(
            "SELECT",
            Identifier("Id", "Id"),
            "FROM",
            "Users",
            EOF()
            ), Tokens(tokens));
    }
}

public class TokenBuilder
{
    public static SyntaxToken EOF()
        => new SyntaxToken(SyntaxKind.EndOfFileToken, string.Empty, null, 0);
    public static SyntaxToken Keyword(string text)
        => new SyntaxToken(Lexer.GetKeywordKind(text), text, null, 0);
    public static SyntaxToken Star()
        => new SyntaxToken(SyntaxKind.StarToken, "*", null, 0);
    public static SyntaxToken Identifier(string text, string? value = null)
        => new SyntaxToken(SyntaxKind.IdentifierToken, text, value, 0);
    public static SyntaxToken String(string text)
        => new SyntaxToken(SyntaxKind.StringToken, text, text, 0);
    public static SyntaxToken Number(string text)
        => new SyntaxToken(SyntaxKind.NumberToken, text, null, 0);
    public static SyntaxToken Number(long value)
        => new SyntaxToken(SyntaxKind.NumberToken, value.ToString(), value, 0);
    public static SyntaxToken Number(double value)
        => new SyntaxToken(SyntaxKind.NumberToken, value.ToString(), value, 0);
    public static SyntaxToken OpenParen()
        => new SyntaxToken(SyntaxKind.OpenParenToken, "(", null, 0);
    public static SyntaxToken CloseParen()
        => new SyntaxToken(SyntaxKind.CloseParenToken, ")", null, 0);
    public static SyntaxToken Comma()
        => new SyntaxToken(SyntaxKind.CommaToken, ",", null, 0);
    public static SyntaxToken Dot()
        => new SyntaxToken(SyntaxKind.DotToken, ".", null, 0);
    public static SyntaxToken EqualToken()
        => new SyntaxToken(SyntaxKind.EqualsToken, "=", null, 0);

    public static List<TokenHelper> Tokens(IEnumerable<object> tokens) => tokens.Select(TokenHelper.Convert).ToList();
    public static List<TokenHelper> Tokens(params object[] tokens) => tokens.Select(TokenHelper.Convert).ToList();

    [DebuggerDisplay("{")]
    public readonly struct TokenHelper : IEquatable<TokenHelper>
    {
        private SyntaxToken Token { get; }

        public TokenHelper(SyntaxToken token)
        {
            Token = token;
        }

        public static TokenHelper Convert(object t) => t switch
        {
            SyntaxToken st => new TokenHelper(st),
            string s => (TokenHelper)s,
            long n => (TokenHelper)n,
            int n => (TokenHelper)n,
            double n => (TokenHelper)n,
            float n => (TokenHelper)n,
            _ => throw new System.InvalidCastException($"Cannot convert {t.GetType().Name} to TokenHelper")
        };

        public static implicit operator TokenHelper(SyntaxToken t) => new TokenHelper(t);
        public static implicit operator TokenHelper(string d)
        {
            return d.ToUpperInvariant() switch
            {
                "." => new TokenHelper(Dot()),
                "," => new TokenHelper(Comma()),
                "(" => new TokenHelper(OpenParen()),
                ")" => new TokenHelper(CloseParen()),
                "=" => new TokenHelper(EqualToken()),
                "*" => new TokenHelper(Star()),
                _ => new TokenHelper(Keyword(d))
            };
        }
        public static implicit operator TokenHelper(int n) => new TokenHelper(Number(n));
        public static implicit operator TokenHelper(long n) => new TokenHelper(Number(n));
        public static implicit operator TokenHelper(double n) => new TokenHelper(Number(n));
        public static implicit operator TokenHelper(float n) => new TokenHelper(Number(n));

        public bool Equals(TokenHelper other)
        {
            return Token.Kind == other.Token.Kind && Token.Text == other.Token.Text && Equals(Token.Value, other.Token.Value);
        }
        public override string ToString()
        {
            return $"{Token.Kind}: {Token.Value ?? Token.Text}";
        }
    }
}

//public class BasicTokenComparer : IEqualityComparer<SyntaxToken>
//{
//    public bool Equals(SyntaxToken x, SyntaxToken y)
//    {
//        return x.Kind == y.Kind && x.Text == y.Text && Equals(x.Value, y.Value);
//    }

//    public int GetHashCode([DisallowNull] SyntaxToken obj)
//    {
//        throw new System.NotImplementedException();
//    }
//}
