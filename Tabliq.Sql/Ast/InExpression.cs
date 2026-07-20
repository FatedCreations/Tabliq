using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class InExpression : Expression
{
    public InExpression(Expression subValue, Expression expression)
    {
        SubValue = subValue;
        Expression = expression;
    }
    public Expression SubValue { get; }
    public Expression Expression { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return SubValue;
        yield return Expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is InExpression otherIn && SubValue.Equals(otherIn.SubValue) && Expression.Equals(otherIn.Expression);
    }
}
