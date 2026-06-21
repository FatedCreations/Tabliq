using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class NamedTableReference : TableReference
{
    public NamedTableReference(IdentifierExpression identifer, string? alias) : base(alias)
    {
        Identifer = identifer;
    }

    public IdentifierExpression Identifer { get; }

    public TableSymbol? Binding { get; set; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Identifer;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is NamedTableReference identifier && Identifer.Equals(identifier.Identifer);
    }
}
