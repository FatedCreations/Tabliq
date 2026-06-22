using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BinaryComparisonCondition : Condition
{
    public BinaryComparisonCondition(Expression left, BinaryCompararisonOperator @operator, Expression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public Expression Left { get; }
    public BinaryCompararisonOperator Operator { get; }
    public Expression Right { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not BinaryComparisonCondition otherBinary)
            return false;
        return Left.Equals(otherBinary.Left) && Operator == otherBinary.Operator && Right.Equals(otherBinary.Right);
    }
}

public enum BinaryCompararisonOperator
{
    Unknown,
    Equals,
    NotEquals,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual
}