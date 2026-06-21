using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class AsExpression : Expression
{
    public AsExpression(Expression expression, string alias)
    {
        Expression = expression;
        Alias = alias;
    }
    public Expression Expression { get; }
    public string Alias { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is AsExpression otherAs && Expression.Equals(otherAs.Expression) && Alias == otherAs.Alias;
    }
}
