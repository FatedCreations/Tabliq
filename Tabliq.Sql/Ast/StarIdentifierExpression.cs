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

    public (string? TableName, string? SchemaName) GetColumnParts()
    {
        if (IdentifierParts.Count == 0)
        {
            return (null, null);
        }
        else if(IdentifierParts.Count == 1)
        {
            return (IdentifierParts[0], null);
        }
        else if (IdentifierParts.Count == 2)
        {
            return (IdentifierParts[1], IdentifierParts[0]);
        }
        else
        {
            throw new InvalidOperationException($"Invalid identifier parts: {string.Join(".", IdentifierParts)}");
        }
    }
}
