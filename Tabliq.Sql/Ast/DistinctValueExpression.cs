using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class DistinctValueExpression : Expression
{
    public Distinctness Distinctness { get; }
    public Expression Expression { get; }

    public DistinctValueExpression(Distinctness distinctness, Expression expression)
    {
        Distinctness = distinctness;
        Expression = expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is DistinctValueExpression dve && dve.Distinctness == Distinctness && Expression.Equals(dve.Expression);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}

public enum Distinctness
{
    Unspecified,
    Distinct,
    All
}