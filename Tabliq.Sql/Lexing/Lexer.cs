using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;

namespace Tabliq.Sql.Lexing;

public sealed class Lexer
{
    private readonly string _text;
    private readonly List<SyntaxToken> _tokens = new();
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    public DiagnosticBag Diagnostics => _diagnostics;

    public Lexer(string text)
    {
        _text = text ?? string.Empty;
        _position = 0;
    }

    private char Current => _position >= _text.Length ? '\0' : _text[_position];

    private void Next() => _position++;

    public IReadOnlyList<SyntaxToken> LexAll()
    {
        while (true)
        {
            var start = _position;

            if (char.IsWhiteSpace(Current))
            {
                Next();
                continue;
            }

            if (Current == '\0')
            {
                _tokens.Add(new SyntaxToken(SyntaxKind.EndOfFileToken, string.Empty, null, _position));
                break;
            }

            if (char.IsLetter(Current) || Current == '_')
            {
                var sb = new StringBuilder();
                while (char.IsLetterOrDigit(Current) || Current == '_')
                {
                    sb.Append(Current);
                    Next();
                }

                var text = sb.ToString();
                var kind = GetKeywordKind(text);
                _tokens.Add(new SyntaxToken(kind, text, null, start));
                continue;
            }

            if (char.IsDigit(Current))
            {
                var sb = new StringBuilder();
                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Next();
                }

                var text = sb.ToString();
                if (long.TryParse(text, out var v))
                    _tokens.Add(new SyntaxToken(SyntaxKind.NumberToken, text, v, start));
                else
                    _tokens.Add(new SyntaxToken(SyntaxKind.NumberToken, text, null, start));
                continue;
            }

            switch (Current)
            {
                case ';':
                    _tokens.Add(new SyntaxToken(SyntaxKind.SemicolonToken, ";", null, start));
                    Next();
                    break;
                case ',':
                    _tokens.Add(new SyntaxToken(SyntaxKind.CommaToken, ",", null, start));
                    Next();
                    break;
                case '.':
                    _tokens.Add(new SyntaxToken(SyntaxKind.DotToken, ".", null, start));
                    Next();
                    break;
                case '*':
                    _tokens.Add(new SyntaxToken(SyntaxKind.StarToken, "*", null, start));
                    Next();
                    break;
                case '(':
                    _tokens.Add(new SyntaxToken(SyntaxKind.OpenParenToken, "(", null, start));
                    Next();
                    break;
                case ')':
                    _tokens.Add(new SyntaxToken(SyntaxKind.CloseParenToken, ")", null, start));
                    Next();
                    break;
                case '+':
                    _tokens.Add(new SyntaxToken(SyntaxKind.PlusToken, "+", null, start));
                    Next();
                    break;
                case '-':
                    Next();
                    if (Current == '-')
                    {
                        while (Current != '\n' && Current != '\0')
                        {
                            Next();
                        }
                        // just swallow the comments just like other whitespace, don't add a token for it
                        break;
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.MinusToken, "-", null, start));
                    }
                    break;
                case '/':
                    _tokens.Add(new SyntaxToken(SyntaxKind.SlashToken, "/", null, start));
                    Next();
                    break;
                case '=':
                    _tokens.Add(new SyntaxToken(SyntaxKind.EqualsToken, "=", null, start));
                    Next();
                    break;
                case '<':
                    Next();
                    if (Current == '=')
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.LessOrEqualsToken, "<=", null, start));
                        Next();
                    }
                    else if (Current == '>')
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.NotEqualsToken, "<>", null, start));
                        Next();
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.LessToken, "<", null, start));
                    }
                    break;
                case '!':
                    Next();
                    if (Current == '=')
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.NotEqualsToken, "!=", null, start));
                        Next();
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.BadToken, "!", null, start));
                    }
                    break;
                case '>':
                    Next();
                    if (Current == '=')
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.GreaterOrEqualsToken, ">=", null, start));
                        Next();
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.GreaterToken, ">", null, start));
                    }
                    break;
                case '|':
                    Next();
                    if (Current == '|')
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.PipePipeToken, "||", null, start));
                        Next();
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.BadToken, "|", null, start));
                    }
                    break;
                case '\'':
                    var quote = Current;
                    Next();
                    var sb = new StringBuilder();
                    while (Current != quote && Current != '\0')
                    {
                        sb.Append(Current);
                        Next();
                    }

                    if (Current == quote)
                    {
                        Next();
                        _tokens.Add(new SyntaxToken(SyntaxKind.StringToken, sb.ToString(), sb.ToString(), start));
                    }
                    else
                    {
                        _diagnostics.Report("UnterminatedString", "Unterminated string literal", start, _position - start);
                        _tokens.Add(new SyntaxToken(SyntaxKind.StringToken, sb.ToString(), sb.ToString(), start));
                    }
                    break;
                case '"':
                case '`':
                case '[':
                    var quoteIdent = Current;
                    var closeIdent = Current switch
                    {
                        '[' => ']',
                        _ => Current
                    };

                    Next();
                    var sbIdent = new StringBuilder();
                    while (Current != closeIdent && Current != '\0')
                    {
                        sbIdent.Append(Current);
                        Next();
                    }

                    if (Current == closeIdent)
                    {
                        Next();
                        _tokens.Add(new SyntaxToken(SyntaxKind.IdentifierToken, sbIdent.ToString(), sbIdent.ToString(), start));
                    }
                    else
                    {
                        _diagnostics.Report("UnterminatedIdentifier", "Unterminated identifier literal", start, _position - start);
                        _tokens.Add(new SyntaxToken(SyntaxKind.IdentifierToken, sbIdent.ToString(), sbIdent.ToString(), start));
                    }
                    break;
                case '@':
                    var sbParam = new StringBuilder();
                    Next();
                    while (char.IsLetterOrDigit(Current) || Current == '_')
                    {
                        sbParam.Append(Current);
                        Next();
                    }

                    var text = sbParam.ToString();
                    if (text.Length == 0)
                    {
                        _diagnostics.Report("BadParameter", "Parameter name cannot be empty", start, _position - start);
                        _tokens.Add(new SyntaxToken(SyntaxKind.BadToken, '@' + text, null, start));
                    }
                    else
                    {
                        _tokens.Add(new SyntaxToken(SyntaxKind.ParameterToken, '@' + text, text, start));
                    }
                    break;
                default:
                    _diagnostics.Report("BadCharacter", $"Bad character: {Current}", start, 1);
                    _tokens.Add(new SyntaxToken(SyntaxKind.BadToken, Current.ToString(), null, start));
                    Next();
                    break;
            }
        }

        return _tokens;
    }

    internal static SyntaxKind GetKeywordKind(string text)
    {
        return text.ToUpperInvariant() switch
        {
            "SELECT" => SyntaxKind.SelectKeyword,
            "FROM" => SyntaxKind.FromKeyword,
            "WHERE" => SyntaxKind.WhereKeyword,
            "WITH" => SyntaxKind.WithKeyword,
            "AS" => SyntaxKind.AsKeyword,
            "IS" => SyntaxKind.IsKeyword,
            "IN" => SyntaxKind.InKeyword,
            "UNION" => SyntaxKind.UnionKeyword,
            "ALL" => SyntaxKind.AllKeyword,
            "TOP" => SyntaxKind.TopKeyword,
            "DISTINCT" => SyntaxKind.DistinctKeyword,
            "JOIN" => SyntaxKind.JoinKeyword,
            "ON" => SyntaxKind.OnKeyword,
            "OVER" => SyntaxKind.OverKeyword,
            "ORDER" => SyntaxKind.OrderKeyword,
            "PARTITION" => SyntaxKind.PartitionKeyword,
            "BY" => SyntaxKind.ByKeyword,
            "GROUP" => SyntaxKind.GroupKeyword,
            "HAVING" => SyntaxKind.HavingKeyword,
            "EXISTS" => SyntaxKind.ExistsKeyword,
            "ROW" => SyntaxKind.RowKeyword,
            "RANGE" => SyntaxKind.RangeKeyword,
            "BETWEEN" => SyntaxKind.BetweenKeyword,
            "OF" => SyntaxKind.OfKeyword,

            "OFFSET" => SyntaxKind.OffsetKeyword,
            "ROWS" => SyntaxKind.RowsKeyword,
            "FETCH" => SyntaxKind.FetchKeyword,
            "NEXT" => SyntaxKind.NextKeyword,
            "FIRST" => SyntaxKind.FirstKeyword,
            "ONLY" => SyntaxKind.OnlyKeyword,
            "MAX" => SyntaxKind.MaxKeyword,
            
            // directions
            "DESC" => SyntaxKind.DescKeyword,
            "ASC" => SyntaxKind.AscKeyword,

            // Logical operators
            "OR" => SyntaxKind.OrKeyword,
            "AND" => SyntaxKind.AndKeyword,

            "NOT" => SyntaxKind.NotKeyword,
            "NULL" => SyntaxKind.NullKeyword,
            "LIKE" => SyntaxKind.LikeKeyword,
            "CASE" => SyntaxKind.CaseKeyword,
            "WHEN" => SyntaxKind.WhenKeyword,
            "THEN" => SyntaxKind.ThenKeyword,
            "ELSE" => SyntaxKind.ElseKeyword,
            "END" => SyntaxKind.EndKeyword,

            "INNER" => SyntaxKind.InnerKeyword,
            "OUTER" => SyntaxKind.OuterKeyword,
            "LEFT" => SyntaxKind.LeftKeyword,
            "RIGHT" => SyntaxKind.RightKeyword,
            "FULL" => SyntaxKind.FullKeyword,
            "CROSS" => SyntaxKind.CrossKeyword,

            "CURRENT_TIMESTAMP" => SyntaxKind.CurrentTimestampKeyword,
            "CURRENT_DATE" => SyntaxKind.CurrentDateKeyword,
            "CURRENT_TIME" => SyntaxKind.CurrentTimeKeyword,

            "CHARACTER" => SyntaxKind.CharacterDataType,
            "BINARY" => SyntaxKind.BinaryDataType,
            "BOOLEAN" => SyntaxKind.BooleanDataType,
            "INTEGER" => SyntaxKind.IntegerDataType,
            "INT" => SyntaxKind.IntegerDataType,
            "SINGLE" => SyntaxKind.SingleDataType,
            "FLOAT" => SyntaxKind.FloatDataType,
            "DOUBLE" => SyntaxKind.DoubleDataType,
            "REAL" => SyntaxKind.RealDataType,
            "DECIMAL" => SyntaxKind.DecimalDataType,
            "DATE" => SyntaxKind.DateDataType,
            "TIME" => SyntaxKind.TimeDataType,
            "TIMESTAMP" => SyntaxKind.TimestampDataType,
            "DATETIME" => SyntaxKind.TimestampDataType,
            "CHAR" => SyntaxKind.CharDataType,
            "VARCHAR" => SyntaxKind.VarcharDataType,
            "NCHAR" => SyntaxKind.NcharDataType,
            "NVARCHAR" => SyntaxKind.NvarcharDataType,
            "UNIQUEIDENTIFIER" => SyntaxKind.UniqueidentifierDataType,

            

            _ => SyntaxKind.IdentifierToken
        };
    }
}
