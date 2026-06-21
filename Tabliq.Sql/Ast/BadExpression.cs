using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BadExpression : Expression
{
    public BadExpression(SyntaxTokenSpan span)
    {
        Span = span;
    }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is BadExpression otherBad && Span.Equals(otherBad.Span);
    }
}
