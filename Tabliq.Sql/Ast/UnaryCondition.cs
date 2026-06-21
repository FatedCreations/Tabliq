using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class UnaryCondition : Condition
{
    public UnaryCondition(UnaryCompararisonOperator @operator, Expression right)
    {
        Operator = @operator;
        Right = right;
    }

    public UnaryCompararisonOperator Operator { get; }
    public Expression Right { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Right;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not UnaryCondition otherUnary)
            return false;
        return Operator == otherUnary.Operator && Right.Equals(otherUnary.Right);
    }
}

public enum UnaryCompararisonOperator
{
    Unknown,
    Not,
}