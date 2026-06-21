using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BadTableReference : TableReference
{
    public BadTableReference(SyntaxTokenSpan span) : base(null)
    {
        Span = span;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is BadTableReference bad && Span.Equals(bad.Span);
    }
}