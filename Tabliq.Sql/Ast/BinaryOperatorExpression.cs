using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BinaryOperatorExpression : Expression
{
    public BinaryOperatorExpression(Expression left, BinaryOperator @operator, Expression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public Expression Left { get; }
    public BinaryOperator Operator { get; }
    public Expression Right { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not BinaryOperatorExpression otherBinary)
            return false;
        return Left.Equals(otherBinary.Left) && Operator == otherBinary.Operator && Right.Equals(otherBinary.Right);
    }
}

public enum BinaryOperator
{
    Unknown,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulus,

    Concatenate,
    // bitshift?
    // logical operators?
}