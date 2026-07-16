using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class EmptyStatement : Statement
{
    public EmptyStatement(bool hasSemicolon = false) : base(hasSemicolon)
    {
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is EmptyStatement emptyStatement &&
               HasSemicolon == emptyStatement.HasSemicolon;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}
