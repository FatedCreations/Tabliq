using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class StarIdentifierExpression : Expression
{
    public StarIdentifierExpression(IEnumerable<string> identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }
    public StarIdentifierExpression(params string[] identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }

    public IReadOnlyList<ColumnBinding> Bindings { get; set; } = [];

    public IReadOnlyList<string> IdentifierParts { get; }

    public string Column => IdentifierParts.Last();

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is StarIdentifierExpression otherIdentifier && IdentifierParts.SequenceEqual(otherIdentifier.IdentifierParts);
    }
}
