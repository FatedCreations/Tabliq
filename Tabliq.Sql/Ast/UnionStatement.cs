using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class UnionStatement : SyntaxNode
{
    public UnionStatement(bool isAll, SelectExpression select)
    {
        Select = select;
        IsAll = isAll;
    }
    public SelectExpression Select { get; }
    public bool IsAll { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Select;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is UnionStatement statement &&
               Select.Equals(statement.Select) &&
               IsAll == statement.IsAll;
    }
}
