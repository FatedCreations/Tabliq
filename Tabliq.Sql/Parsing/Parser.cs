using System.Net.Http.Headers;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Lexing;

namespace Tabliq.Sql.Parsing;

public sealed class Parser
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

    private bool TryMatchTokens(ReadOnlySpan<SyntaxKind> kinds)
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
            var bad = ConsumeUntil(k => k.Kind == SyntaxKind.SelectKeyword || k.Kind == SyntaxKind.WithKeyword);
            if (bad != null)
                statements.Add(bad);
            if (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var statment = ParseSelectStatement();
                statements.Add(statment);
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

        return new SelectStatement(ctes, selectExpression).WithLocation(loc);
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

    private Condition ParseCondition()
    {
        var conditionLoc = Track();
        var primary = ParsePrimaryCondition();
        while (Current.Kind == SyntaxKind.AndKeyword || Current.Kind == SyntaxKind.OrKeyword)
        {
            var opToken = NextToken();
            var op = GetLogicalOperator(opToken.Kind);
            var right = ParseCondition();
            // todo: correctly handle precedence here to enable future processing order of operations, for now we will just treat it as left associative
            primary = new LogicalCondition(primary, op, right).WithLocation(conditionLoc);
        }
        return primary;
    }

    private LogicalOperator GetLogicalOperator(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.AndKeyword => LogicalOperator.And,
            SyntaxKind.OrKeyword => LogicalOperator.Or,
            _ => throw new NotImplementedException()
        };
    }

    private Condition ParsePrimaryCondition()
    {
        var loc = Track();
        if (IsUnaryComparisonOperator(Current.Kind))
        {
            var opToken = NextToken();
            var op = GetUnaryComparisonOperator(opToken.Kind);
            var right = ParseCondition();
            return new UnaryCondition(op, right).WithLocation(loc);
        }

        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            // bracketed select expression
            NextToken(); // consume '('
            var exp = ParseCondition();
            MatchToken(SyntaxKind.CloseParenToken);
            return new BracketedCondition(exp).WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.ExistsKeyword))
        {
            MatchToken(SyntaxKind.OpenParenToken);
            var selectExpression = ParseSelectExpression();
            MatchToken(SyntaxKind.CloseParenToken);

            return new ExistsCondition(new SelectExpression(false, selectExpression)).WithLocation(loc);
        }

        var left = ParseExpression();
        if (IsBinaryComparisonOperator(Current.Kind))
        {
            var opToken = NextToken();
            var op = GetBinaryComparisonOperator(opToken.Kind);
            var right = ParseExpression();
            return new BinaryComparisonCondition(left, op, right).WithLocation(loc);
        }

        if (IsMatch([SyntaxKind.NotKeyword, SyntaxKind.LikeKeyword, SyntaxKind.StringToken]) || IsMatch([SyntaxKind.LikeKeyword, SyntaxKind.StringToken]))
        {
            var isNot = TryMatchToken(SyntaxKind.NotKeyword);
            MatchToken(SyntaxKind.LikeKeyword);//like
            var stringToken = MatchToken(SyntaxKind.StringToken);//like

            // is not null
            return new LikeCondition(isNot,
                left,
                new LiteralExpression(stringToken.Text).WithLocation(stringToken))
                .WithLocation(loc);
        }

        if (TryMatchTokens([SyntaxKind.IsKeyword, SyntaxKind.NotKeyword, SyntaxKind.NullKeyword]))
        {
            // is not null
            return new IsNullCondition(true, left).WithLocation(loc);
        }

        if (TryMatchTokens([SyntaxKind.IsKeyword, SyntaxKind.NullKeyword]))
        {
            // is not null
            return new IsNullCondition(false, left).WithLocation(loc);
        }

        _diagnostics.Report("ExpectedCondition", "Expected a condition operator (=, !=, <, >, <=, >=)", Current.Start, Current.Text.Length);
        return new BadCondition(loc.Span).WithLocation(loc);
    }

    private bool IsBinaryComparisonOperator(SyntaxKind kind)
        => GetBinaryComparisonOperator(kind) != BinaryCompararisonOperator.Unknown;
    private BinaryCompararisonOperator GetBinaryComparisonOperator(SyntaxKind kind)
        => kind switch
        {
            SyntaxKind.EqualsToken => BinaryCompararisonOperator.Equals,
            SyntaxKind.NotEqualsToken => BinaryCompararisonOperator.NotEquals,
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

    private Expression ParseExpression(bool enableAsExpressions = false)
    {
        var loc = Track();
        var left = ParsePrimaryExpression();

        while (IsBinaryOperator(Current.Kind))
        {
            var opToken = NextToken();
            var op = GetBinaryOperator(opToken.Kind);
            var right = ParsePrimaryExpression();
            left = new BinaryOperatorExpression(left, op, right).WithLocation(loc);
        }


        return left;
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
        if (IsMatch(0, SyntaxKind.OpenParenToken) && IsMatch(1, SyntaxKind.NumberToken, SyntaxKind.MaxKeyword) && IsMatch(2, SyntaxKind.CloseParenToken))
        {
            MatchToken(SyntaxKind.OpenParenToken);
            length = MatchToken(SyntaxKind.NumberToken)?.Value?.ToString() ?? MatchToken(SyntaxKind.MaxKeyword).Text.ToUpperInvariant();
            MatchToken(SyntaxKind.CloseParenToken);
        }
        return new DataType(dataType.Text, length).WithLocation(loc);
    }

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
    private BinaryOperator GetBinaryOperator(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.PlusToken => BinaryOperator.Add,
            SyntaxKind.MinusToken => BinaryOperator.Subtract,
            SyntaxKind.StarToken => BinaryOperator.Multiply,
            SyntaxKind.SlashToken => BinaryOperator.Divide,
            SyntaxKind.PipePipeToken => BinaryOperator.Concatenate,
            _ => BinaryOperator.Unknown
        };
    }

    private bool IsBinaryOperator(SyntaxKind kind)
        => GetBinaryOperator(kind) != BinaryOperator.Unknown;

    private FunctionCallExpression ParseFunctionExpression()
    {
        if (!IsIdentifierPart(Current.Kind))
        {
            ReportWrongToken("Indentifier");
        }

        var loc = Track();
        var functionName = NextToken().Text;
        MatchToken(SyntaxKind.OpenParenToken);

        List<Expression> arguments = new List<Expression>();
        Distinctness distinctness = Distinctness.Unspecified;
        string? fromExpressionType = null;

        if (Current.Kind != SyntaxKind.CloseParenToken)
        {
            // process arguments!!
            do
            {
                var argLocation = Track();
                if (arguments.Count == 0)
                {
                    if (Current.Kind == SyntaxKind.DistinctKeyword || Current.Kind == SyntaxKind.AllKeyword)
                    {
                        distinctness = NextToken().Kind switch
                        {
                            SyntaxKind.DistinctKeyword => Distinctness.Distinct,
                            SyntaxKind.AllKeyword => Distinctness.All,
                            _ => throw new Exception("Cannot be reached")
                        };

                    }

                    // can't mix and match
                    if (distinctness is Distinctness.Unspecified)
                    {
                        if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.FromKeyword)
                        {
                            // process Extract(foo from bar) style expressions
                            fromExpressionType = NextToken().Text;
                            NextToken(); // consum from
                        }
                    }
                }

                var expression = ParseExpression();

                // as expression can only be the first argument of a function CONVERT() PARSE() and TRY_PARSE() are the only functions that support AS expression and they only support it for the first argument
                if (arguments.Count == 0)
                {
                    if (Current.Kind == SyntaxKind.AsKeyword && IsDataType(Peek(1).Kind))
                    {
                        NextToken(); // consume 'AS'
                        var dataType = ParseDataType();
                        expression = new AsExpression(expression, dataType).WithLocation(argLocation);
                    }
                }

                if (fromExpressionType != null)
                {
                    expression = new ValueFromExpression(fromExpressionType, expression).WithLocation(argLocation);
                }

                if (distinctness != Distinctness.Unspecified)
                {
                    expression = new DistinctValueExpression(distinctness, expression).WithLocation(argLocation);
                }

                arguments.Add(expression);

            } while (TryMatchToken(SyntaxKind.CommaToken));
        }

        MatchToken(SyntaxKind.CloseParenToken);

        WindowSpecification? window = null;
        if (Current.Kind == SyntaxKind.OverKeyword)
        {
            var windowLoc = Track();
            NextToken(); // read 'over'
            MatchToken(SyntaxKind.OpenParenToken);

            List<Expression> partitions = new List<Expression>();
            // partition by
            if (TryMatchTokens([SyntaxKind.PartitionKeyword, SyntaxKind.ByKeyword]))
            {
                // comma separated list until Order by
                do
                {
                    var expression = ParseExpression();
                    partitions.Add(expression);
                }
                while (TryMatchToken(SyntaxKind.CommaToken));
            }
            OrderByClause? orderBy = null;
            if (IsMatch([SyntaxKind.OrderKeyword, SyntaxKind.ByKeyword]))
            {
                orderBy = ParseOrderBy();
            }

            //todo: add "Frame Clause" (range or rows clause) https://en.wikipedia.org/wiki/Window_function_(SQL)

            MatchToken(SyntaxKind.CloseParenToken);

            window = new WindowSpecification(partitions, orderBy).WithLocation(windowLoc);

        }

        return new FunctionCallExpression(functionName, arguments, window).WithLocation(loc);
    }

    private Expression ParseIdentifierExpression()
    {
        var loc = Track();
        List<string> identifiers = new List<string>();
        do
        {
            var token = NextToken();
            if (token.Kind == SyntaxKind.StarToken)
            {
                return new StarIdentifierExpression(identifiers).WithLocation(loc);
            }

            identifiers.Add(token.Text);

            if (!IsIdentifierPart(token.Kind))
            {
                ReportWrongToken(SyntaxKind.IdentifierToken);
                break;
            }
        } while (TryMatchToken(SyntaxKind.DotToken));

        return new IdentifierExpression(identifiers).WithLocation(loc);
    }

    private bool IsIdentifierPart(SyntaxKind kind)
        => kind is SyntaxKind.IdentifierToken || IsDataType(kind);

    private Expression ParseCaseExpression()
    {
        var loc = Track();
        MatchToken(SyntaxKind.CaseKeyword);

        // are we dealing with a simple case expression or a searched case expression?

        Expression? source = null;
        if (Current.Kind != SyntaxKind.WhenKeyword)
        {
            // simple case expression
            source = ParseExpression();
        }

        List<CaseWhenClause> whenClauses = new List<CaseWhenClause>();
        var whenLoc = Track();
        while (TryMatchToken(SyntaxKind.WhenKeyword))
        {
            var term = source is null ? ParseCondition() : ParseExpression();
            MatchToken(SyntaxKind.ThenKeyword);
            var result = ParseExpression();
            whenClauses.Add(new CaseWhenClause(term, result).WithLocation(whenLoc));
            whenLoc = Track();
        }

        Expression? elseExpression = null;
        if (TryMatchToken(SyntaxKind.ElseKeyword))
        {
            elseExpression = ParseExpression();
        }

        MatchToken(SyntaxKind.EndKeyword);

        return new CaseExpression(source, whenClauses, elseExpression).WithLocation(loc);
    }

    private Expression ParsePrimaryExpression()
    {
        var loc = Track();
        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            if (Peek(1).Kind == SyntaxKind.SelectKeyword)
            {
                var selectExpression = ParseSelectExpression();
                return selectExpression;
            }
            // bracketed select expression
            NextToken(); // consume '('
            var exp = ParsePrimaryExpression();
            MatchToken(SyntaxKind.CloseParenToken);
            return new BracketedExpression(exp).WithLocation(loc);
        }

        if (Current.Kind == SyntaxKind.ParameterToken)
        {
            var paramName = NextToken().Value as string;
            return new ParameterIdentifier(paramName!).WithLocation(loc);
        }
        if (Current.Kind == SyntaxKind.CaseKeyword)
        {
            return ParseCaseExpression();
        }
        if (Current.Kind == SyntaxKind.StringToken)
        {
            var stringToken = NextToken();
            return new LiteralExpression(stringToken.Value).WithLocation(loc);
        }

        if (Current.Kind == SyntaxKind.NumberToken)
        {
            var integerPart = (long)NextToken().Value!;

            if (IsMatch([SyntaxKind.DotToken, SyntaxKind.NumberToken]))
            {
                NextToken(); // consume '.'
                var fractionalToken = MatchToken(SyntaxKind.NumberToken);
                var fractionalPart = (long)fractionalToken.Value!;
                var decimalValue = ((double)fractionalPart / Math.Pow(10, fractionalToken.Text.Length)) + integerPart;
                return new LiteralExpression(decimalValue).WithLocation(loc);
            }

            return new LiteralExpression(integerPart).WithLocation(loc);
        }

        // handle all the other word based tokens as identifiers
        if (IsIdentifierPart(Current.Kind) || Current.Kind == SyntaxKind.StarToken)
        {
            if (Current.Kind != SyntaxKind.StarToken && Peek(1).Kind == SyntaxKind.OpenParenToken)
            {
                return ParseFunctionExpression();
            }

            return ParseIdentifierExpression();
        }

        //if (Current.Kind == SyntaxKind.OpenParenToken)
        //{
        //    NextToken(); // consume '(';
        //    var expression = ParseExpression();
        //    expression = new BracketedExpression(expression);
        //    MatchToken(SyntaxKind.CloseParenToken);
        //    return expression;
        //}

        _diagnostics.Report("UnexpectedToken", "Unexpected token in expression", Current.Start, Current.Text.Length);
        return new BadExpression(loc.Span).WithLocation(loc);
    }
}
