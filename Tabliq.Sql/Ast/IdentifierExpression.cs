using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class IdentifierExpression : Expression
{
    public static IdentifierExpression FromTableSymbol(TableSymbol tableSymbol)
    {
        if (string.IsNullOrEmpty(tableSymbol.SchemaName))
        {
            return new IdentifierExpression(tableSymbol.TableName);
        }
        return new IdentifierExpression(tableSymbol.SchemaName, tableSymbol.TableName);
    }

    public IdentifierExpression(IEnumerable<string> identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }
    public IdentifierExpression(params string[] identifierParts)
    {
        IdentifierParts = identifierParts.ToList();
    }

    public IReadOnlyList<string> IdentifierParts { get; }

    public (string? TableName, string? SchemaName, string ColumnName) GetColumnParts()
    {
        if (IdentifierParts.Count == 1)
        {
            return (null, null, IdentifierParts[0]);
        }
        else if (IdentifierParts.Count == 2)
        {
            return (IdentifierParts[0], null, IdentifierParts[1]); 
        }
        else if (IdentifierParts.Count == 3)
        {
            return (IdentifierParts[1], IdentifierParts[0], IdentifierParts[2]);
        }
        else
        {
            throw new InvalidOperationException($"Invalid identifier parts: {string.Join(".", IdentifierParts)}");
        }
    }


    public (string TableName, string? SchemaName) GetTableParts()
    {
        if (IdentifierParts.Count == 1)
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
