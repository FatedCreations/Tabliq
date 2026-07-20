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
