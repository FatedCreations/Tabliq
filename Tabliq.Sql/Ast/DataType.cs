using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class DataType : SyntaxNode
{
    public DataType(string name, string? length = null, string? precision = null, string? scale = null)
    {
        Name = name;
        Length = length;
        Precision = precision;
        Scale = scale;
    }

    public string Name { get; }
    public string? Length { get; }
    public string? Precision { get; }
    public string? Scale { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is DataType otherType && Name == otherType.Name && Length == otherType.Length && Precision == otherType.Precision && Scale == otherType.Scale;
    }
}