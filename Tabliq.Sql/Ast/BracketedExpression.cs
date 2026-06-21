using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BracketedExpression : Expression
{
    public BracketedExpression(Expression expression)
    {
        Expression = expression;
    }

    public Expression Expression { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is BracketedExpression otherBracketed && Expression.Equals(otherBracketed.Expression);
    }
}
