using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class WithinGroupClause : SyntaxNode
{
    public WithinGroupClause(OrderByClause? orderBy)
    {
        OrderBy = orderBy;
    }

    public OrderByClause? OrderBy { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is WithinGroupClause window &&
            (OrderBy is null && window.OrderBy is null || OrderBy?.Equals(window.OrderBy) == true);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (OrderBy != null)
        {
            yield return OrderBy;
        }
    }
}