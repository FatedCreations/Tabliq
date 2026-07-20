using System.Net.Http.Headers;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Lexing;

namespace Tabliq.Sql.Parsing;

public sealed partial class Parser
{
    public static CompilationResult Parse(string text)
    {
        var lexer = new Lexer(text);
        var tokens = lexer.LexAll();
        var parser = new Parser(tokens, lexer.Diagnostics);
        var root = parser.ParseCompilationUnit();
        var diags = new List<Diagnostic>();
        diags.AddRange(lexer.Diagnostics.Diagnostics);
        diags.AddRange(parser.Diagnostics.Diagnostics);
        return new CompilationResult(text, root, tokens, diags);
    }

    private readonly List<SyntaxToken> _tokens;
    private int _position;
    private readonly DiagnosticBag _diagnostics;

    public DiagnosticBag Diagnostics => _diagnostics;

    public Parser(IReadOnlyList<SyntaxToken> tokens, DiagnosticBag diagnostics)
    {
        _tokens = tokens.ToList();
        _position = 0;
        _diagnostics = new DiagnosticBag();
        _diagnostics.AddRange(diagnostics.Diagnostics);
    }

    private string DebugString
    {
        get
        {
            var startPos = Math.Max(0, _position - 5);
            var endPos = Math.Min(_tokens.Count, _position + 15);
            var sb = new StringBuilder();
            for (var i = startPos; i < endPos; i++)
            {
                if (i > startPos)
                {
                    sb.Append(" ");
                }
                if (i == _position)
                {
                    sb.Append("|>");
                }
                sb.Append(_tokens[i].Text);

                if (i == _position)
                {
                    sb.Append("<|");
                }
            }
            return sb.ToString();
        }
    }
    private SyntaxToken Current => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Count)
            return _tokens.Last();
        return _tokens[index];
    }

    internal struct LocationTracker(Parser p, int pos)
    {
        SyntaxToken GetToken(int pos)
        {
            var index = pos;
            if (index >= p._tokens.Count)
                return p._tokens.Last();
            if (index < 0)
                index = 0;
            return p._tokens[index];
        }

        public bool HasTokens => p._position > pos;

        public SyntaxTokenSpan Span => new SyntaxTokenSpan(GetToken(pos), GetToken(p._position - 1)); //minus 1?
    }

    private LocationTracker Track() => new LocationTracker(this, _position);

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();

        _diagnostics.Report("UnexpectedToken", $"Expected token {kind} but found {Current.Kind}", Current.Start, Current.Text.Length);
        return new SyntaxToken(kind, string.Empty, null, Current.Start);
    }
    private SyntaxToken MatchToken(Func<SyntaxKind, bool> predicate, string entity)
    {
        if (predicate(Current.Kind))
            return NextToken();

        _diagnostics.Report("UnexpectedToken", $"Expected {entity} but found {Current.Kind}", Current.Start, Current.Text.Length);
        return new SyntaxToken(Current.Kind, string.Empty, null, Current.Start);
    }

    private void ReportWrongToken(SyntaxKind kind)
    {
        _diagnostics.Report("UnexpectedToken", $"Expected token {kind} but found {Current.Kind}", Current.Start, Current.Text.Length);
    }

    private void ReportWrongToken(string expectedType)
    {
        _diagnostics.Report("UnexpectedToken", $"Expected {expectedType} but found {Current.Kind}", Current.Start, Current.Text.Length);
    }

    private bool TryMatchToken(SyntaxKind kind)
    {
        if (Current.Kind == kind)
        {
            NextToken();
            return true;
        }

        return false;
    }

    private bool TryMatchTokens(params ReadOnlySpan<SyntaxKind> kinds)
    {
        for (var i = 0; i < kinds.Length; i++)
        {
            if (Peek(i).Kind != kinds[i])
            {
                return false;
            }
        }

        foreach (var kind in kinds)
        {
            NextToken();
        }

        return true;
    }

    private bool IsMatch(int offset, params ReadOnlySpan<SyntaxKind> kinds)
    {
        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] != Peek(i + offset).Kind)
                return false;
        }
        return true;
    }
    private bool IsMatch(params ReadOnlySpan<SyntaxKind> kinds)
        => IsMatch(0, kinds);

    public SqlScript ParseCompilationUnit()
    {
        var loc = Track();
        List<Statement> statements = new List<Statement>();
        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            var bad = ConsumeUntil(k => k.Kind == SyntaxKind.SelectKeyword || k.Kind == SyntaxKind.WithKeyword || k.Kind == SyntaxKind.SemicolonToken);
            if (bad != null)
                statements.Add(bad);
            if (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var statment = ParseStatement();
                if (statment is not null)
                {
                    statements.Add(statment);
                }
            }
        }
        return new SqlScript(statements).WithLocation(loc);
    }

    private BadStatement? ConsumeUntil(Func<SyntaxToken, bool> predicate)
    {
        var loc = Track();
        while (Current.Kind != SyntaxKind.EndOfFileToken && !predicate(Current))
        {
            NextToken();
        }

        if (!loc.HasTokens)
            return null;

        _diagnostics.Report("UnexpectedToken", $"'{loc.Span.StartToken.Text}' was unexpected", loc.Span);

        return new BadStatement(loc.Span);
    }

    private Statement? ParseStatement()
    {
        if (IsMatch(SyntaxKind.SelectKeyword) || IsMatch(SyntaxKind.WithKeyword))
        {
            return ParseSelectStatement();
        }
        else if (TryMatchToken(SyntaxKind.SemicolonToken))
        {
            var loc = Track();
            return new EmptyStatement(true).WithLocation(loc);
        }

        return null;
    }

    private SelectStatement ParseSelectStatement()
    {
        var loc = Track();
        List<CommonTableExpression> ctes = [];
        if (Current.Kind == SyntaxKind.WithKeyword)
        {
            NextToken();

            do
            {
                var cteLoc = Track();
                var alias = MatchToken(SyntaxKind.IdentifierToken); // CTE name
                MatchToken(SyntaxKind.AsKeyword);
                MatchToken(SyntaxKind.OpenParenToken);

                var select = ParseSelectExpression();
                ctes.Add(new CommonTableExpression(alias.Text, select).WithLocation(cteLoc));

                MatchToken(SyntaxKind.CloseParenToken);
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    NextToken();
                }
                else { break; }
            } while (true);
        }
        var selectExpression = ParseSelectExpression();

        bool hasSemicolon = TryMatchToken(SyntaxKind.SemicolonToken);

        return new SelectStatement(ctes, selectExpression, hasSemicolon).WithLocation(loc);
    }

    private SelectExpression ParseSelectExpression()
    {
        var loc = Track();
        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            // bracketed select expression
            NextToken(); // consume '('
            var selectExpression = ParseSelectExpression();
            MatchToken(SyntaxKind.CloseParenToken);
            return new SelectExpression(true, selectExpression).WithLocation(loc);
        }

        MatchToken(SyntaxKind.SelectKeyword);

        long? top = null;

        if (Current.Kind == SyntaxKind.TopKeyword && Peek(1).Kind == SyntaxKind.NumberToken)
        {
            NextToken(); // consume 'TOP'
            var topToken = MatchToken(SyntaxKind.NumberToken);
            top = (long?)topToken.Value;
        }

        Distinctness distinctness = Distinctness.Unspecified;
        if (Current.Kind == SyntaxKind.DistinctKeyword)
        {
            NextToken(); // consume 'DISTINCT'
            distinctness = Distinctness.Distinct;
        }
        else if (Current.Kind == SyntaxKind.AllKeyword)
        {
            NextToken(); // consume 'ALL'
            distinctness = Distinctness.All;
        }

        var projections = new List<SelectProjection>();

        do
        {
            var selProjLoc = Track();
            Expression? expression = ParseExpression();

            string? alias = null;
            bool isSynthetic = false;
            if (TryMatchToken(SyntaxKind.AsKeyword))
            {
                alias = MatchToken(IsIdentifierPart, "identifier").Text;
            }
            else
            {
                // we haven't consumed the trailing alias yet so make sure its not there, just missing the AS keyword
                if (Current.Kind == SyntaxKind.IdentifierToken)
                {
                    alias = NextToken().Text;
                }
            }

            isSynthetic = alias is null;

            if (expression is IdentifierExpression idExp)
            {
                alias = alias ?? idExp.Column;
            }

            if (expression is not null)
            {
                projections.Add(new SelectProjection(expression, alias, isSynthetic).WithLocation(selProjLoc));
            }
            else
            {
                _diagnostics.Report("ExpectedExpression", "Expected identifier or expression", Current.Start, Current.Text.Length);
            }
        } while (TryMatchToken(SyntaxKind.CommaToken));

        FromClause? fromClause = null;
        if (TryMatchToken(SyntaxKind.FromKeyword))
        {
            var fromLoc = Track();
            List<TableReference> tableReferences = new List<TableReference>();
            List<JoinClause> joins = new List<JoinClause>();

            do
            {
                tableReferences.Add(ParseTableReference());

            } while (TryMatchToken(SyntaxKind.CommaToken));

            // try to consume as many joins as possible, until we hit a token that is not a join type
            while (true)
            {
                var joinLoc = Track();
                // it can be
                if (TryMatchTokens([SyntaxKind.CrossKeyword, SyntaxKind.JoinKeyword]))
                {
                    var source = ParseTableReference();
                    joins.Add(new JoinClause(JoinSide.Unspecified, JoinType.Cross, source, null).WithLocation(joinLoc));
                }
                else if (TryMatchTokens([SyntaxKind.InnerKeyword, SyntaxKind.JoinKeyword]))
                {
                    var source = ParseTableReference();
                    MatchToken(SyntaxKind.OnKeyword);
                    var onCondition = ParseCondition();
                    joins.Add(new JoinClause(JoinSide.Unspecified, JoinType.Inner, source, onCondition).WithLocation(joinLoc));
                }
                else
                {
                    // LEFT OUTER JOIN;
                    // RIGHT OUTER JOIN;
                    // FULL OUTER JOIN;
                    // LEFT JOIN;
                    // RIGHT JOIN;
                    // FULL JOIN;

                    var (side, isOuter) = Current switch
                    {
                        _ when TryMatchTokens([SyntaxKind.LeftKeyword, SyntaxKind.OuterKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Left, true),
                        _ when TryMatchTokens([SyntaxKind.LeftKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Left, false),
                        _ when TryMatchTokens([SyntaxKind.RightKeyword, SyntaxKind.OuterKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Right, true),
                        _ when TryMatchTokens([SyntaxKind.RightKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Right, false),
                        _ when TryMatchTokens([SyntaxKind.FullKeyword, SyntaxKind.OuterKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Full, true),
                        _ when TryMatchTokens([SyntaxKind.FullKeyword, SyntaxKind.JoinKeyword])
                        => (JoinSide.Full, false),
                        _ when TryMatchTokens([SyntaxKind.JoinKeyword])
                        => (JoinSide.Unspecified, false),
                        _ => ((JoinSide?)null, false)
                    };

                    if (side.HasValue)
                    {
                        side ??= JoinSide.Unspecified;

                        var source = ParseTableReference();

                        MatchToken(SyntaxKind.OnKeyword);
                        var onCondition = ParseCondition();
                        joins.Add(new JoinClause(side.Value, isOuter ? JoinType.Outer : JoinType.Unspecified, source, onCondition).WithLocation(joinLoc));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            fromClause = new FromClause(tableReferences, joins).WithLocation(fromLoc);
        }

        WhereClause? whereClause = null;
        if (Current.Kind == SyntaxKind.WhereKeyword)
        {
            var whereLoc = Track();
            NextToken(); // consume 'WHERE'
            var condition = ParseCondition();
            whereClause = new WhereClause(condition).WithLocation(whereLoc);
        }
        // group by
        GroupByClause? groupByClause = null;
        if (IsMatch([SyntaxKind.GroupKeyword, SyntaxKind.ByKeyword]))
        {
            groupByClause = ParseGroupBy();
        }

        // having 
        HavingClause? havingClause = null;
        if (IsMatch([SyntaxKind.HavingKeyword]))
        {
            var havingLoc = Track();
            NextToken(); // consume 'HavingClause'
            var condition = ParseCondition();
            havingClause = new HavingClause(condition).WithLocation(havingLoc);
        }

        var unions = new List<UnionStatement>();
        if (Current.Kind == SyntaxKind.UnionKeyword)
        {
            var unionLoc = Track();
            NextToken(); // consume 'UNION'
            bool isAll = false;
            if (Current.Kind == SyntaxKind.AllKeyword)
            {
                NextToken(); // consume 'ALL'
                isAll = true;
            }

            var selectExpression = ParseSelectExpression();
            unions.Add(new UnionStatement(isAll, selectExpression).WithLocation(unionLoc));
        }

        // have to process unions first, can't have an order by before a union
        OrderByClause? orderByClause = null;
        if (IsMatch([SyntaxKind.OrderKeyword, SyntaxKind.ByKeyword]))
        {
            orderByClause = ParseOrderBy();
        }

        return new SelectExpression(false, top, distinctness, projections, fromClause, whereClause, groupByClause, havingClause, orderByClause, unions).WithLocation(loc);
    }

    private TableReference ParseTableReference()
    {
        var loc = Track();
        Expression? sourceExpression = null;
        bool aliasRequired = false;
        string? alias = null;

        if (Current.Kind is SyntaxKind.IdentifierToken)
        {
            sourceExpression = ParseIdentifierExpression();
        }
        else if (Current.Kind is SyntaxKind.OpenParenToken)
        {
            aliasRequired = true;
            sourceExpression = ParseSelectExpression();
        }

        if (Current.Kind is SyntaxKind.AsKeyword)
        {
            NextToken();
            aliasRequired = true;
        }
        if (aliasRequired)
        {
            alias = MatchToken(SyntaxKind.IdentifierToken).Text;
        }
        else if (Current.Kind is SyntaxKind.IdentifierToken)
        {
            alias = NextToken().Text;
        }

        if (sourceExpression is SelectExpression selectExpression)
        {
            return new SelectTableReference(selectExpression, alias).WithLocation(loc);
        }
        else if (sourceExpression is IdentifierExpression identifierExpression)
        {
            return new NamedTableReference(identifierExpression, alias).WithLocation(loc);
        }
        else
        {
            return new BadTableReference(loc.Span).WithLocation(loc);
        }
    }

    public IEnumerable<Expression> ParseBracketedList()
    {
        List<Expression> items = new List<Expression>();
        MatchToken(SyntaxKind.OpenParenToken);
        do
        {
            var item = ParseExpression();
            items.Add(item);
        } while (TryMatchToken(SyntaxKind.CommaToken));
        MatchToken(SyntaxKind.CloseParenToken);
        return items;
    }

    private bool IsBinaryComparisonOperator(SyntaxKind kind)
        => GetBinaryComparisonOperator(kind) != BinaryCompararisonOperator.Unknown;

    private BinaryCompararisonOperator GetBinaryComparisonOperator(SyntaxKind kind)
        => kind switch
        {
            SyntaxKind.EqualsToken => BinaryCompararisonOperator.Equals,
            SyntaxKind.NotEqualsToken => BinaryCompararisonOperator.NotEquals,
            SyntaxKind.NotEqualsAlt1Token => BinaryCompararisonOperator.NotEquals,
            SyntaxKind.LessToken => BinaryCompararisonOperator.LessThan,
            SyntaxKind.GreaterToken => BinaryCompararisonOperator.GreaterThan,
            SyntaxKind.LessOrEqualsToken => BinaryCompararisonOperator.LessThanOrEqual,
            SyntaxKind.GreaterOrEqualsToken => BinaryCompararisonOperator.GreaterThanOrEqual,
            _ => BinaryCompararisonOperator.Unknown
        };

    private bool IsUnaryComparisonOperator(SyntaxKind kind)
        => GetUnaryComparisonOperator(kind) != UnaryCompararisonOperator.Unknown;
    private UnaryCompararisonOperator GetUnaryComparisonOperator(SyntaxKind kind)
        => kind switch
        {
            SyntaxKind.NotKeyword => UnaryCompararisonOperator.Not,
            _ => UnaryCompararisonOperator.Unknown
        };

    private GroupByClause ParseGroupBy()
    {
        var loc = Track();
        MatchToken(SyntaxKind.GroupKeyword);
        MatchToken(SyntaxKind.ByKeyword);
        var entries = new List<Expression>();
        do
        {
            var exp = ParseExpression();
            entries.Add(exp);

        } while (TryMatchToken(SyntaxKind.CommaToken));

        return new GroupByClause(entries).WithLocation(loc);
    }

    private OrderByClause? TryParseOrderBy()
    {
        if (IsMatch([SyntaxKind.OrderKeyword, SyntaxKind.ByKeyword]))
        {
            return ParseOrderBy();
        }

        return null;
    }

    private OrderByClause ParseOrderBy()
    {
        var loc = Track();
        MatchToken(SyntaxKind.OrderKeyword);
        MatchToken(SyntaxKind.ByKeyword);
        var entries = new List<OrderByEntry>();
        do
        {
            var entryLoc = Track();
            var exp = ParseExpression();
            OrderByDirection dir = OrderByDirection.Unspecified;

            if (Current.Kind == SyntaxKind.DescKeyword || Current.Kind == SyntaxKind.AscKeyword)
            {
                dir = NextToken().Kind == SyntaxKind.DescKeyword ? OrderByDirection.Descending : OrderByDirection.Ascending;
            }
            entries.Add(new OrderByEntry(exp, dir).WithLocation(entryLoc));

        } while (TryMatchToken(SyntaxKind.CommaToken));
        // we now parse expression with options DESC ASC or DESCENDING ASCENDING

        OffsetClause? offsetClause = null;
        if (TryMatchToken(SyntaxKind.OffsetKeyword))
        {
            var offsetLoc = Track();
            var offsetCount = ParseExpression();
            TryMatchToken(SyntaxKind.RowKeyword);// optional keyword that mean nothing, just consume if they exist
            TryMatchToken(SyntaxKind.RowsKeyword);// optional keyword that mean nothing, just consume if they exist

            Expression? fetchCount = null;
            if (TryMatchToken(SyntaxKind.FetchKeyword))
            {
                TryMatchToken(SyntaxKind.FirstKeyword);// optional keyword that mean nothing, just consume if they exist
                TryMatchToken(SyntaxKind.NextKeyword); // optional keyword that mean nothing, just consume if they exist
                fetchCount = ParseExpression();
                TryMatchToken(SyntaxKind.RowKeyword);// optional keyword that mean nothing, just consume if they exist
                TryMatchToken(SyntaxKind.RowsKeyword);// optional keyword that mean nothing, just consume if they exist
                TryMatchToken(SyntaxKind.OnlyKeyword);// optional keyword that mean nothing, just consume if they exist
            }
            offsetClause = new OffsetClause(offsetCount, fetchCount).WithLocation(offsetLoc);
        }

        return new OrderByClause(entries, offsetClause).WithLocation(loc);
    }

    private DataType ParseDataType()
    {
        var loc = Track();
        if (!IsDataType(Current.Kind))
        {
            ReportWrongToken("any data type");
            return new DataType("").WithLocation(loc);
        }

        var dataType = NextToken(); // consume data type
        string? length = null;
        if (IsMatch(0, SyntaxKind.OpenParenToken) && (IsMatch(1, SyntaxKind.NumberToken) || IsMatch(1, SyntaxKind.MaxKeyword)) && IsMatch(2, SyntaxKind.CloseParenToken))
        {
            MatchToken(SyntaxKind.OpenParenToken);
            var token = NextToken();

            length = token.Value?.ToString() ?? token.Text.ToUpperInvariant();

            MatchToken(SyntaxKind.CloseParenToken);
        }

        return new DataType(dataType.Text, length).WithLocation(loc);
    }

    private bool IsKeyword(SyntaxKind kind)
        => kind is
            SyntaxKind.SelectKeyword or
            SyntaxKind.FromKeyword or
            SyntaxKind.WhereKeyword or
            SyntaxKind.WithKeyword or
            SyntaxKind.AsKeyword or
            SyntaxKind.IsKeyword or
            SyntaxKind.InKeyword or
            SyntaxKind.UnionKeyword or
            SyntaxKind.AllKeyword or
            SyntaxKind.DistinctKeyword or
            SyntaxKind.TopKeyword or
            SyntaxKind.JoinKeyword or
            SyntaxKind.OnKeyword or
            SyntaxKind.OverKeyword or
            SyntaxKind.WithinKeyword or
            SyntaxKind.OrderKeyword or
            SyntaxKind.PartitionKeyword or
            SyntaxKind.ByKeyword or
            SyntaxKind.GroupKeyword or
            SyntaxKind.HavingKeyword or
            SyntaxKind.RowKeyword or
            SyntaxKind.RangeKeyword or
            SyntaxKind.OffsetKeyword or
            SyntaxKind.RowsKeyword or
            SyntaxKind.FetchKeyword or
            SyntaxKind.NextKeyword or
            SyntaxKind.FirstKeyword or
            SyntaxKind.OnlyKeyword or
            SyntaxKind.AscKeyword or
            SyntaxKind.DescKeyword or
            SyntaxKind.InnerKeyword or
            SyntaxKind.OuterKeyword or
            SyntaxKind.LeftKeyword or
            SyntaxKind.RightKeyword or
            SyntaxKind.FullKeyword or
            SyntaxKind.CrossKeyword or
            SyntaxKind.OrKeyword or
            SyntaxKind.AndKeyword or
            SyntaxKind.NotKeyword or
            SyntaxKind.NullKeyword or
            SyntaxKind.LikeKeyword or
            SyntaxKind.CaseKeyword or
            SyntaxKind.WhenKeyword or
            SyntaxKind.ThenKeyword or
            SyntaxKind.ElseKeyword or
            SyntaxKind.EndKeyword or
            SyntaxKind.PipePipeToken or
            SyntaxKind.ExistsKeyword or
            SyntaxKind.MaxKeyword or
            SyntaxKind.BetweenKeyword or
            SyntaxKind.OfKeyword;

    private bool IsDataType(SyntaxKind kind)
        => kind is
            SyntaxKind.CharacterDataType or
            SyntaxKind.BinaryDataType or
            SyntaxKind.BooleanDataType or
            SyntaxKind.IntegerDataType or
            SyntaxKind.SingleDataType or
            SyntaxKind.FloatDataType or
            SyntaxKind.DoubleDataType or
            SyntaxKind.RealDataType or
            SyntaxKind.DecimalDataType or
            SyntaxKind.DateDataType or
            SyntaxKind.TimeDataType or
            SyntaxKind.TimestampDataType or
            SyntaxKind.CharDataType or
            SyntaxKind.VarcharDataType or
            SyntaxKind.NcharDataType or
            SyntaxKind.NvarcharDataType or
            SyntaxKind.UniqueidentifierDataType;
}
