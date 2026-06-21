using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class SelectTableReference : TableReference
{
    public SelectTableReference(SelectExpression select, string? alias) : base(alias)
    {
        Select = select;
    }

    public SelectExpression Select { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Select;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is SelectTableReference subSelect && Select.Equals(subSelect.Select);
    }
}
