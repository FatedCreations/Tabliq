using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class WindowSpecification : SyntaxNode
{
    public WindowSpecification(OverClause? over, WithinGroupClause? withinGroup)
    {
        Over = over;
        WithinGroup = withinGroup;
    }

    public OverClause? Over { get; }

    public WithinGroupClause? WithinGroup { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is WindowSpecification window &&
            (Over is null && window.Over is null || Over?.Equals(window.Over) == true) &&
            (WithinGroup is null && window.WithinGroup is null || WithinGroup?.Equals(window.WithinGroup) == true);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (WithinGroup != null)
        {
            yield return WithinGroup;
        }
        if (Over is not null)
        {
            yield return Over;
        }
    }
}
