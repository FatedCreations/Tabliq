using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class GroupByClause : SyntaxNode
{
    public GroupByClause(IEnumerable<Expression> entries)
    {
        Entries = new List<Expression>(entries);
    }

    public IReadOnlyList<Expression> Entries { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is GroupByClause groupBy && Entries.SyntaxSequenceEqual(groupBy.Entries);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var entry in Entries)
        {
            yield return entry;
        }
    }
}
