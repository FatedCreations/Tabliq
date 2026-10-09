namespace Tabliq.Execution.ExpressionPlan;

public sealed class IsNullConditionExecutionPlan: ConditionExecutionPlan
{
    public IsNullConditionExecutionPlan(ExpressionPlanNode expression, bool isNot)
    {
        Expression = expression;
        IsNot = isNot;
    }

    public ExpressionPlanNode Expression { get; }
    public bool IsNot { get; }

    public override bool Execute(RowAccessor row)
        => Expression.ExecuteExpression(row) is null == !IsNot;
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Expression];
}
