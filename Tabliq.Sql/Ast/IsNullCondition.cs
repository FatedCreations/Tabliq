using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class IsNullCondition : Condition
{
    public IsNullCondition(bool isNot, Expression expression)
    {
        IsNot = isNot;
        Expression = expression;
    }

    public bool IsNot { get; }
    public Expression Expression { get; }
    public override bool Equals(SyntaxNode? other)
    {
        return other is IsNullCondition con && con.IsNot == IsNot && con.Expression.Equals(Expression);
    }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}
