using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class OrderByClause : SyntaxNode
{
    public OrderByClause(IEnumerable<OrderByEntry> entries)
    {
        Entries = new List<OrderByEntry>(entries);
    }

    public IReadOnlyList<OrderByEntry> Entries { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is OrderByClause orderBy && Entries.SyntaxSequenceEqual(orderBy.Entries);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var entry in Entries)
        {
            yield return entry;
        }
    }
}

public class OrderByEntry : SyntaxNode
{
    public OrderByEntry(Expression expression, OrderByDirection direction)
    {
        Expression = expression;
        Direction = direction;
    }

    public Expression Expression { get; }
    public OrderByDirection Direction { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is OrderByEntry entry && Expression.Equals(entry.Expression) && Direction == entry.Direction;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}

public enum OrderByDirection
{
    Unspecified,
    Ascending,
    Descending
}