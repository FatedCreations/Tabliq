using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Rewriter;

namespace Tabliq.RemoteExecuter.MsSql;

internal class RewriteForMsSqlServer : SqlRewiter
{
    public static readonly RewriteForMsSqlServer Instance = new RewriteForMsSqlServer();

    protected override FunctionCallExpression Rewrite(FunctionCallExpression node)
    {
        if (node.FunctionName.Equals("EXTRACT", StringComparison.OrdinalIgnoreCase) && node.Arguments.Count == 1 && node.Arguments[0] is ValueFromExpression from)
        {
            node = new FunctionCallExpression("DATEPART", [
                new IdentifierExpression(from.Part){
                    Span = from.Span
                },
                from.Expression
                ], node.Window)
            {
                Span = node.Span,
                Binding = node.Binding,
            };
        }
        return base.Rewrite(node);
    }

    protected override SyntaxNode Rewrite(SyntaxNode node)
    {
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

        if (node is BinaryOperatorExpression op)
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
}
