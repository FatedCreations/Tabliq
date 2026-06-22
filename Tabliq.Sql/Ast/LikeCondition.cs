using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class LikeCondition : Condition
{
 public LikeCondition(bool isNot, Expression left, Expression right)
    {
        IsNot = isNot;
        Left = left;
        Right = right;
    }

    public bool IsNot { get; }
    public Expression Left { get; }
    public Expression Right { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not LikeCondition otherLike)
            return false;
        return IsNot == otherLike.IsNot && Left.Equals(otherLike.Left) && Right.Equals(otherLike.Right);
    }
}
