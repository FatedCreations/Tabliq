namespace Tabliq.Execution.ExpressionPlan;

public sealed class ConstantConditionExecutionPlan(bool value) : ConditionExecutionPlan
{
    public override bool Execute(RowAccessor row) => value;
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
