namespace Tabliq.Execution.ExpressionPlan;

public sealed class BetweenConditionExecutionPlan : ConditionExecutionPlan
{
    public BetweenConditionExecutionPlan(ExpressionPlanNode value, ExpressionPlanNode from, ExpressionPlanNode to, bool isNot)
    {
        Value = value;
        From = from;
        To = to;
        IsNot = isNot;
    }

    public ExpressionPlanNode Value { get; }
    public ExpressionPlanNode From { get; }
    public ExpressionPlanNode To { get; }
    public bool IsNot { get; }


    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value, From, To];

    public override bool Execute(RowAccessor row)
    {
        var value = Value.ExecuteExpression(row);
        var from = From.ExecuteExpression(row);
        var to = To.ExecuteExpression(row);

        var result = ComparisonHelpers.Compare(value, from) >= 0 && ComparisonHelpers.Compare(value, to) <= 0;
        return IsNot ? !result : result;
    }
}
