namespace Tabliq.Execution.ExpressionPlan;

public sealed class InListConditionExecutionPlan: ConditionExecutionPlan
{
    public InListConditionExecutionPlan(ExpressionPlanNode left, IEnumerable<ExpressionPlanNode> items, bool isNot)
    {
        Left = left;
        Items = items;
        IsNot = isNot;
    }

    public ExpressionPlanNode Left { get; }
    public IEnumerable<ExpressionPlanNode> Items { get; }
    public bool IsNot { get; }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, ..Items];
    public override bool Execute(RowAccessor row)
    {
        var left = Left.ExecuteExpression(row);
        var result = false;
        foreach (var item in Items)
        {
            if (Equals(left, item.ExecuteExpression(row)))
            {
                result = true;
                break;
            }
        }

        return IsNot ? !result : result;
    }
}
