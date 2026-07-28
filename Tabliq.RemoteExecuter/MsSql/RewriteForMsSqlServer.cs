using System.Xml.Linq;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Rewriter;

namespace Tabliq.RemoteExecuter.MsSql;

internal class RewriteForMsSqlServer : SqlRewiter
{
    public static readonly RewriteForMsSqlServer Instance = new RewriteForMsSqlServer();

    public Dictionary<string, string> FunctionNameRewrites { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["STDDEV_POP"] = "STDEV",
        ["STDDEV_SAMP"] = "STDEVP",
        ["VAR_POP"] = "VAR",
        ["VAR_SAMP"] = "VARP",
        ["CHAR_LENGTH"] = "LEN",
        ["CHARACTER_LENGTH"] = "LEN",
        ["CEIL"] = "CEILING",
        ["LN"] = "LOG",
    };

    protected override FunctionCallExpression Rewrite(FunctionCallExpression node)
    {
        if (FunctionNameRewrites.TryGetValue(node.FunctionName, out var newName))
        {
            // simple aliasing of function names.
            return new FunctionCallExpression(newName, node.Arguments, node.Window);
        }
        else if (node.FunctionName.Equals("EXTRACT", StringComparison.OrdinalIgnoreCase) && node.Arguments.Count == 1 && node.Arguments[0] is ValueFromExpression from)
        {
            node = new FunctionCallExpression(
                "DATEPART",
                [
                    new IdentifierExpression(from.Part)
                    {
                        Span = from.Span
                    },
                    from.Expression
                ],
                node.Window)
            {
                Span = node.Span,
                Binding = node.Binding,
            };
        }
        else if (node.FunctionName.Equals("POSITION", StringComparison.OrdinalIgnoreCase) && node.Arguments.Count == 1 && node.Arguments[0] is InExpression inExp)
        {
            node = new FunctionCallExpression(
                "CHARINDEX",
                [
                    inExp.SubValue,
                    inExp.Expression
                ],
                node.Window)
            {
                Span = node.Span,
                Binding = node.Binding,
            };
        }

        return base.Rewrite(node);
    }

    protected override SyntaxNode Rewrite(SyntaxNode node)
    {
        if (TryCreateCovarSamp(node, out node) || TryCreateCovarPop(node, out node))
        {
            // already handled
        }
        else if (node is FunctionCallExpression c && c.FunctionName.Equals("MOD", StringComparison.OrdinalIgnoreCase) && c.Arguments.Count == 2)
        {
            node = new BinaryOperatorExpression(c.Arguments[0], BinaryOperator.Modulus, c.Arguments[1])
            {
                Span = node.Span,
            };
        }
        else if (node is CurrentTimestamp)
        {
            node = new FunctionCallExpression("GETDATE", [], null)
            {
                Span = node.Span,
            };
        }
        else if (node is CurrentTime)
        {
            node = new FunctionCallExpression(
                "CAST",
                [
                    new AsExpression(
                        new FunctionCallExpression("GETDATE", [], null)
                        {
                            Span = node.Span,
                        },
                        new DataType("TIME")
                        {
                            Span = node.Span,
                        })
                ],
                null)
            {
                Span = node.Span,
            };
        }
        else if (node is CurrentDate)
        {
            node = new FunctionCallExpression(
                "CAST",
                [
                    new AsExpression(
                        new FunctionCallExpression("GETDATE", [], null)
                        {
                            Span = node.Span,
                        },
                        new DataType("DATE")
                        {
                            Span = node.Span,
                        })
                ],
                null)
            {
                Span = node.Span,
            };
        }

        IEnumerable<Expression> GetConcatinateExpressions(Expression node)
        {
            if (node is BinaryOperatorExpression bin && bin.Operator == BinaryOperator.Concatenate)
            {
                foreach (var expr in GetConcatinateExpressions(bin.Left))
                {
                    yield return expr;
                }

                foreach (var expr in GetConcatinateExpressions(bin.Right))
                {
                    yield return expr;
                }
            }
            else
            {
                yield return node;
            }
        }

        if (node is BinaryOperatorExpression op && op.Operator == BinaryOperator.Concatenate)
        {
            if (GetConcatinateExpressions(op).Any())
            {
                node = new FunctionCallExpression("CONCAT", GetConcatinateExpressions(op), null)
                {
                    Span = op.Span,
                };
            }
        }

        return base.Rewrite(node);
    }

    private bool TryCreateCovarPop(SyntaxNode node, out SyntaxNode result)
    {
        result = node;
        if (node is not FunctionCallExpression c || !c.FunctionName.Equals("COVAR_POP", StringComparison.OrdinalIgnoreCase) || c.Arguments.Count != 2)
        {
            return false;
        }
        var x = c.Arguments[0];
        var y = c.Arguments[1];

        result = CreateCovarPop(x, y);
        return true;
    }
    private Expression CreateCovarPop(Expression x, Expression y)
    {
        var caseExp = new CaseExpression(
          null,
          [
              new CaseWhenClause(
                    new LogicalCondition(
                        new IsNullCondition(true, x),
                        LogicalOperator.And,
                        new IsNullCondition(true, y)),
                    new BinaryOperatorExpression(x, BinaryOperator.Multiply, y))
          ],
          null);
        var caseExpX = new CaseExpression(null, [new CaseWhenClause(new IsNullCondition(true, y), x)], null);
        var caseExpY = new CaseExpression(null, [new CaseWhenClause(new IsNullCondition(true, x), y)], null);

        var avg1 = new FunctionCallExpression("AVG", [caseExp], null);
        var avg2 = new FunctionCallExpression("AVG", [caseExpX], null);
        var avg3 = new FunctionCallExpression("AVG", [caseExpY], null);

        return new BinaryOperatorExpression(avg1, BinaryOperator.Subtract, new BracketedExpression(new BinaryOperatorExpression(avg2, BinaryOperator.Multiply, avg3)));
    }

    private bool TryCreateCovarSamp(SyntaxNode node, out SyntaxNode result)
    {
        result = node;
        if (node is not FunctionCallExpression c || !c.FunctionName.Equals("COVAR_SAMP", StringComparison.OrdinalIgnoreCase) || c.Arguments.Count != 2)
        {
            return false;
        }
        var x = c.Arguments[0];
        var y = c.Arguments[1];

        var coVarPop = CreateCovarPop(x, y);
        var count = new FunctionCallExpression("COUNT", [
            new CaseExpression(null, [
                new CaseWhenClause(new LogicalCondition(new IsNullCondition(true, x), LogicalOperator.And, new IsNullCondition(true, y)), new LiteralExpression(1))], null)], null);
        var nullifCount = new FunctionCallExpression("NULLIF", [new BinaryOperatorExpression(count, BinaryOperator.Subtract, new LiteralExpression(1)), new LiteralExpression(0)], null);
        result =
            new BinaryOperatorExpression(
            new BracketedExpression(
            new BinaryOperatorExpression(count, BinaryOperator.Multiply, coVarPop)
            ), BinaryOperator.Divide, nullifCount);

        return true;
    }
}
