using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class OffsetClause : SyntaxNode
{
    public OffsetClause(Expression offsetCount, Expression? fetchCount = null)
    {
        OffsetCount = offsetCount;
        FetchCount = fetchCount;
    }

    public Expression OffsetCount { get; }
    public Expression? FetchCount { get; } = null;

    public override bool Equals(SyntaxNode? other)
    {
        return other is OffsetClause offset && OffsetCount.Equals(offset.OffsetCount) && Equals(FetchCount, offset.FetchCount);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return OffsetCount;
        if (FetchCount != null)
        {
            yield return FetchCount;
        }
    }
}

public class OrderByClause : SyntaxNode
{
    public OrderByClause(IEnumerable<OrderByEntry> entries, OffsetClause? offsetClause)
    {
        Entries = new List<OrderByEntry>(entries);
        OffsetClause = offsetClause;
    }

    public IReadOnlyList<OrderByEntry> Entries { get; }
    public OffsetClause? OffsetClause { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is OrderByClause orderBy && Entries.SyntaxSequenceEqual(orderBy.Entries) && Equals(OffsetClause, orderBy.OffsetClause);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var entry in Entries)
        {
            yield return entry;
        }

        if (OffsetClause != null)
        {
            yield return OffsetClause;
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