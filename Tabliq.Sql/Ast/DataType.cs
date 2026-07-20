using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class DataType : SyntaxNode
{
    public DataType(string name, string? size = null)
    {
        Name = name;
        Size = size;
    }

    public string Name { get; }

    public string? Size { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is DataType otherType && Name == otherType.Name && Size == otherType.Size;
    }
}