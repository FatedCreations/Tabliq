using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public abstract class Statement : SyntaxNode
{
    protected Statement(bool hasSemicolon = false)
    {
        HasSemicolon = hasSemicolon;
    }

    public bool HasSemicolon { get; }
}
