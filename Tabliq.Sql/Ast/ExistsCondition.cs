using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class ExistsCondition : Condition
{
    public ExistsCondition(SelectExpression selectExpression)
    {
        SelectExpression = selectExpression;
    }

    public SelectExpression SelectExpression { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return SelectExpression;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not ExistsCondition otherExists)
            return false;
        return SelectExpression.Equals(otherExists.SelectExpression);
    }
}
