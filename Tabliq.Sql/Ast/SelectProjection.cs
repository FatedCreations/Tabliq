using System.Data;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class SelectProjection : SyntaxNode
{
    public SelectProjection(Expression expression, string? alias = null, bool isSynthetic = false)
    {
        Expression = expression;
        Alias = alias;
        IsSynthetic = isSynthetic;
    }

    public Expression Expression { get; }

    public string? Alias { get; }

    public bool IsSynthetic { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        if (other is not SelectProjection otherProjection)
            return false;

        return Expression.Equals(otherProjection.Expression) &&
               Alias == otherProjection.Alias &&
               IsSynthetic == otherProjection.IsSynthetic;
    }
}
