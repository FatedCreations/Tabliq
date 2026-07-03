using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class InSelectCondition : Condition
{
    public InSelectCondition(bool isNot, Expression left, SelectExpression expression)
    {
        IsNot = isNot;
        Left = left;
        Expression = expression;
    }

    public bool IsNot { get; }
    public Expression Left { get; }
    public SelectExpression Expression { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Expression;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not InSelectCondition otherInSelect)
            return false;
        return IsNot == otherInSelect.IsNot && Left.Equals(otherInSelect.Left) && Expression.Equals(otherInSelect.Expression);
    }
}

public class InListCondition : Condition
{
    public InListCondition(bool isNot, Expression left, IEnumerable<Expression> items)
    {
        IsNot = isNot;
        Left = left;
        Items = items;
    }
    public bool IsNot { get; }
    public Expression Left { get; }
    public IEnumerable<Expression> Items { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        foreach (var item in Items)
        {
            yield return item;
        }
    }
    override public bool Equals(SyntaxNode? other)
    {
        if (other is not InListCondition otherInList)
            return false;
        return IsNot == otherInList.IsNot && Left.Equals(otherInList.Left) && Items.Equals(otherInList.Items);
    }
}
