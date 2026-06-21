using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public abstract class TableReference : SyntaxNode
{
    protected TableReference(string? alias)
    {
        Alias = alias;
    }

    public string? Alias { get; }

}
