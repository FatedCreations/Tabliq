using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class BetweenCondition : Condition
{
    public BetweenCondition(bool isNot, Expression left, Expression from, Expression to)
    {
        IsNot = isNot;
        Left = left;
        From = from;
        To = to;
    }

    public bool IsNot { get; }
    public Expression Left { get; }
    public Expression From { get; }
    public Expression To { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return From;
        yield return To;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not BetweenCondition otherBetween)
            return false;
        return IsNot == otherBetween.IsNot && Left.Equals(otherBetween.Left) && From.Equals(otherBetween.From) && To.Equals(otherBetween.To);
    }
}
