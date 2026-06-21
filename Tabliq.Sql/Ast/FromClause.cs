using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class FromClause : SyntaxNode
{
    public FromClause(IEnumerable<TableReference> tableReferences, IEnumerable<JoinClause> joins)
    {
        TableReferences = new List<TableReference>(tableReferences);
        Joins = new List<JoinClause>(joins);
    }

    public IReadOnlyList<TableReference> TableReferences { get; } = [];
    public List<JoinClause> Joins { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var table in TableReferences)
        {
            yield return table;
        }
        foreach (var join in Joins)
        {
            yield return join;
        }
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is FromClause from && TableReferences.SyntaxSequenceEqual(from.TableReferences) && Joins.SyntaxSequenceEqual(from.Joins);
    }
}
