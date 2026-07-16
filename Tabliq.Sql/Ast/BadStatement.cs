using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BadStatement : Statement
{
    public BadStatement(SyntaxTokenSpan span) : base(false)
    {
        Span = span;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is BadStatement otherBad && Span.Equals(otherBad.Span);
    }
}
