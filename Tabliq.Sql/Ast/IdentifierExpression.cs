using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class IdentifierExpression : Expression
{
    public IdentifierExpression(IEnumerable<string> identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }
    public IdentifierExpression(params string[] identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }

    public IReadOnlyList<string> IdentifierParts { get; }

    public string Column => IdentifierParts.Last();

    public ColumnBinding? Binding { get; internal set; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is IdentifierExpression otherIdentifier && IdentifierParts.SequenceEqual(otherIdentifier.IdentifierParts);
    }
}
