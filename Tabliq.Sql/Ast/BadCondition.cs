using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BadCondition : Condition
{
    public BadCondition(SyntaxTokenSpan span)
    {
        Span = span;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is BadCondition otherBad && Span.Equals(otherBad.Span);
    }
}
