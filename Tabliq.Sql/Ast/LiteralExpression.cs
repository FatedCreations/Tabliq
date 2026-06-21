using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

// TODO: split this into number literal, string literal, boolean literal and null literal, so that we can have a more precise type system and avoid boxing/unboxing for value types.
public class LiteralExpression : Expression
{
    public LiteralExpression(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is LiteralExpression otherLiteral && Equals(Value, otherLiteral.Value);
    }
}
