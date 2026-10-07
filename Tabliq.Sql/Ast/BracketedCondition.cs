using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BracketedCondition : Condition
{
    public BracketedCondition(Condition expression)
    {
        Expression = expression;
    }

    public Condition Expression { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is BracketedCondition otherBracketed && Expression.Equals(otherBracketed.Expression);
    }
}
