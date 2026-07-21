using System.Net.Http.Headers;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Lexing;

namespace Tabliq.Sql.Parsing;

public sealed partial class Parser
{
    private Expression ParseExpression()
    {
        var expression = ParseExpressionOrCondition();

        if (expression is Condition)
        {
            _diagnostics.Report("UnexpectedCondition", "Expected a simple expression but found a condition", expression.Span);
        }

        return expression;
    }

    private Condition ParseCondition()
    {
        var conditionLoc = Track();
        var expression = ParseExpressionOrCondition();

        if (expression is Condition condition)
        {
            return condition;
        }

        _diagnostics.Report("ExpectedCondition", "Expected a condition but found just an expression", conditionLoc.Span);
        // If the expression is not a condition, wrap it in a default condition
        return new BadCondition(conditionLoc.Span).WithLocation(conditionLoc);
    }

    private Expression ParseExpressionOrCondition()
    {
        var loc = Track();
        var left = ParsePrimaryExpression();

        while (IsBinaryOperator(Current.Kind))
        {
            var opToken = NextToken();
            var op = GetBinaryOperator(opToken.Kind);
            var right = ParseExpressionOrCondition();
            if (right is BinaryComparisonCondition con)
            {
                left = new BinaryOperatorExpression(left, op, con.Left).WithLocation(loc);
                left = new BinaryComparisonCondition(left, con.Operator, con.Right).WithLocation(loc);
            }
            else if (left is BinaryComparisonCondition conLeft)
            {
                right = new BinaryOperatorExpression(conLeft.Right, op, right).WithLocation(loc);
                left = new BinaryComparisonCondition(conLeft.Left, conLeft.Operator, right).WithLocation(loc);
            }
            else
            {
                left = new BinaryOperatorExpression(left, op, right).WithLocation(loc);
            }
        }

        if (IsBinaryComparisonOperator(Current.Kind))
        {
            var opToken = NextToken();
            var op = GetBinaryComparisonOperator(opToken.Kind);
            var right = ParseExpressionOrCondition();
            left = new BinaryComparisonCondition(left, op, right).WithLocation(loc);
        }
        else if (IsMatch([SyntaxKind.NotKeyword, SyntaxKind.BetweenKeyword]) || IsMatch([SyntaxKind.BetweenKeyword]))
        {
            var isNot = TryMatchToken(SyntaxKind.NotKeyword);
            MatchToken(SyntaxKind.BetweenKeyword);//between
            var val1 = ParseExpressionOrCondition();
            MatchToken(SyntaxKind.AndKeyword);
            var val2 = ParseExpressionOrCondition();

            // is not null
            left = new BetweenCondition(isNot,
                left,
                val1,
                val2)
                .WithLocation(loc);
        }
        else if (IsMatch(SyntaxKind.NotKeyword, SyntaxKind.InKeyword, SyntaxKind.OpenParenToken) || IsMatch(SyntaxKind.InKeyword, SyntaxKind.OpenParenToken))
        {
            var isNot = TryMatchToken(SyntaxKind.NotKeyword);
            MatchToken(SyntaxKind.InKeyword);//between

            // this is eather a sub select in brackets or a comma seperated list

            if (IsMatch(SyntaxKind.OpenParenToken, SyntaxKind.SelectKeyword))
            {
                var selectExpression = ParseSelectExpression();
                selectExpression = new SelectExpression(true, selectExpression).WithLocation(loc);
                left = new InSelectCondition(isNot, left, selectExpression).WithLocation(loc);
            }
            else
            {
                var items = ParseBracketedList();
                left = new InListCondition(isNot, left, items).WithLocation(loc);
            }
        }
        else if (IsMatch([SyntaxKind.NotKeyword, SyntaxKind.LikeKeyword, SyntaxKind.StringToken]) || IsMatch([SyntaxKind.LikeKeyword, SyntaxKind.StringToken]))
        {
            var isNot = TryMatchToken(SyntaxKind.NotKeyword);
            MatchToken(SyntaxKind.LikeKeyword);//like
            var stringToken = MatchToken(SyntaxKind.StringToken);//like

            // is not null
            left = new LikeCondition(isNot,
                left,
                new LiteralExpression(stringToken.Text).WithLocation(stringToken))
                .WithLocation(loc);
        }
        else if (TryMatchTokens([SyntaxKind.IsKeyword, SyntaxKind.NotKeyword, SyntaxKind.NullKeyword]))
        {
            // is not null
            left = new IsNullCondition(true, left).WithLocation(loc);
        }
        else if (TryMatchTokens([SyntaxKind.IsKeyword, SyntaxKind.NullKeyword]))
        {
            // is not null
            left = new IsNullCondition(false, left).WithLocation(loc);
        }

        if (left is Condition condition)
        {
            var leftCondition = condition;
            while (Current.Kind == SyntaxKind.AndKeyword || Current.Kind == SyntaxKind.OrKeyword)
            {
                var opToken = NextToken();
                var op = GetLogicalOperator(opToken.Kind);
                var right = ParseCondition();
                // todo: correctly handle precedence here to enable future processing order of operations, for now we will just treat it as left associative
                leftCondition = new LogicalCondition(leftCondition, op, right).WithLocation(loc);
            }

            left = leftCondition;
        }

        return left;
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
            ReportWrongToken("Identifier");
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

                var expression = ParseExpressionOrCondition();

                // as expression can only be the first argument of a function CONVERT() PARSE() and TRY_PARSE() are the only functions that support AS expression and they only support it for the first argument
                if (arguments.Count == 0)
                {
                    if (Current.Kind == SyntaxKind.AsKeyword && IsDataType(Peek(1).Kind))
                    {
                        NextToken(); // consume 'AS'
                        var dataType = ParseDataType();
                        expression = new AsExpression(expression, dataType).WithLocation(argLocation);
                    }
                    else if (Current.Kind == SyntaxKind.InKeyword)
                    {
                        NextToken(); // consume 'IN'
                        var right = ParseExpressionOrCondition();
                        expression = new InExpression(expression, right).WithLocation(argLocation);
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

        var window = TryParseWindowSpecification();

        return new FunctionCallExpression(functionName, arguments, window).WithLocation(loc);
    }

    private WindowSpecification? TryParseWindowSpecification()
    {
        var windowLoc = Track();

        WithinGroupClause? withinGroupClause = null;
        OverClause? overClause = null;

        var withinLoc = Track();
        if (TryMatchTokens(SyntaxKind.WithinKeyword, SyntaxKind.GroupKeyword))
        {
            MatchToken(SyntaxKind.OpenParenToken);

            var withinGroup = TryParseOrderBy();

            withinGroupClause = new WithinGroupClause(withinGroup).WithLocation(withinLoc);

            MatchToken(SyntaxKind.CloseParenToken);
        }

        var overLoc = Track();
        if (TryMatchToken(SyntaxKind.OverKeyword))
        {
            MatchToken(SyntaxKind.OpenParenToken);

            var partitions = new List<Expression>();

            // partition by
            if (TryMatchTokens([SyntaxKind.PartitionKeyword, SyntaxKind.ByKeyword]))
            {
                // comma separated list until Order by
                do
                {
                    var expression = ParseExpressionOrCondition();
                    partitions.Add(expression);
                }
                while (TryMatchToken(SyntaxKind.CommaToken));
            }

            var orderBy = TryParseOrderBy();

            //todo: add "Frame Clause" (range or rows clause) https://en.wikipedia.org/wiki/Window_function_(SQL)

            MatchToken(SyntaxKind.CloseParenToken);

            overClause = new OverClause(partitions, orderBy).WithLocation(overLoc);
        }

        if (overClause is not null || withinGroupClause is not null)
        {
            return new WindowSpecification(overClause, withinGroupClause).WithLocation(windowLoc);
        }

        return null;
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
        => kind is SyntaxKind.IdentifierToken || IsDataType(kind) || IsKeyword(kind);

    private Expression ParseCaseExpression()
    {
        var loc = Track();
        MatchToken(SyntaxKind.CaseKeyword);

        // are we dealing with a simple case expression or a searched case expression?

        Expression? source = null;
        if (Current.Kind != SyntaxKind.WhenKeyword)
        {
            // simple case expression
            source = ParseExpressionOrCondition();
        }

        List<CaseWhenClause> whenClauses = new List<CaseWhenClause>();
        var whenLoc = Track();
        while (TryMatchToken(SyntaxKind.WhenKeyword))
        {
            var term = source is null ? ParseCondition() : ParseExpressionOrCondition();
            MatchToken(SyntaxKind.ThenKeyword);
            var result = ParseExpressionOrCondition();
            whenClauses.Add(new CaseWhenClause(term, result).WithLocation(whenLoc));
            whenLoc = Track();
        }

        Expression? elseExpression = null;
        if (TryMatchToken(SyntaxKind.ElseKeyword))
        {
            elseExpression = ParseExpressionOrCondition();
        }

        MatchToken(SyntaxKind.EndKeyword);

        return new CaseExpression(source, whenClauses, elseExpression).WithLocation(loc);
    }

    private Expression ParsePrimaryExpression()
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
            if (Peek(1).Kind == SyntaxKind.SelectKeyword)
            {
                var selectExpression = ParseSelectExpression();
                return selectExpression;
            }
            // bracketed select expression
            NextToken(); // consume '('
            var exp = ParseExpressionOrCondition();
            MatchToken(SyntaxKind.CloseParenToken);
            if (exp is Condition condition)
            {
                return new BracketedCondition(condition).WithLocation(loc);
            }
            return new BracketedExpression(exp).WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.ExistsKeyword))
        {
            MatchToken(SyntaxKind.OpenParenToken);
            var selectExpression = ParseSelectExpression();
            MatchToken(SyntaxKind.CloseParenToken);

            return new ExistsCondition(new SelectExpression(false, selectExpression)).WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.NullKeyword))
        {
            return new NullValue().WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.CurrentDateKeyword))
        {
            return new CurrentDate().WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.CurrentTimeKeyword))
        {
            return new CurrentTime().WithLocation(loc);
        }

        if (TryMatchToken(SyntaxKind.CurrentTimestampKeyword))
        {
            return new CurrentTimestamp().WithLocation(loc);
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

        if (Current.Kind == SyntaxKind.NumberToken || IsMatch(SyntaxKind.MinusToken, SyntaxKind.NumberToken) || IsMatch(SyntaxKind.PlusToken, SyntaxKind.NumberToken))
        {
            int ngtivness = 1;
            if (Current.Kind != SyntaxKind.NumberToken)
            {
                if (NextToken().Kind == SyntaxKind.MinusToken)
                {
                    ngtivness = -1;
                }
            }
            var integerPart = (long)NextToken().Value!;

            if (IsMatch([SyntaxKind.DotToken, SyntaxKind.NumberToken]))
            {
                NextToken(); // consume '.'
                var fractionalToken = MatchToken(SyntaxKind.NumberToken);
                var fractionalPart = (long)fractionalToken.Value!;
                var decimalValue = ((double)fractionalPart / Math.Pow(10, fractionalToken.Text.Length)) + integerPart;
                return new LiteralExpression(decimalValue * ngtivness).WithLocation(loc);
            }

            return new LiteralExpression(integerPart * ngtivness).WithLocation(loc);
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
