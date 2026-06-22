using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class AsExpression : Expression
{
    public AsExpression(Expression expression, DataType dataType)
    {
        Expression = expression;
        DataType = dataType;
    }
    public Expression Expression { get; }
    public DataType DataType { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        yield return DataType;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is AsExpression otherAs && Expression.Equals(otherAs.Expression) && DataType.Equals(otherAs.DataType);
    }
}

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