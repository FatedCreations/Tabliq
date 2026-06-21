using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class WindowSpecification : SyntaxNode
{
    public WindowSpecification(IEnumerable<Expression> partions, OrderByClause? orderBy)
    {
        Partions = partions;
        OrderBy = orderBy;
    }

    public IEnumerable<Expression> Partions { get; }
    public OrderByClause? OrderBy { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is WindowSpecification window &&
            Partions.SyntaxSequenceEqual(window.Partions) &&
            (OrderBy is null && window.OrderBy is null || OrderBy?.Equals(window.OrderBy) == true);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var partition in Partions)
        {
            yield return partition;
        }
        if (OrderBy != null)
        {
            yield return OrderBy;
        }
    }
}
