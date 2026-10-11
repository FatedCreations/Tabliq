namespace Tabliq.Execution.ExpressionPlan;

public sealed class InSelectConditionExecutionPlan : ConditionExecutionPlan
{
    public ExecutionPlanNode ExecutionPlanNode { get; }
    public ExpressionPlanNode Value { get; }
    public bool IsNot { get; }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. ExecutionPlanNode.GetExpressions()];
    public InSelectConditionExecutionPlan(ExecutionPlanNode executionPlanNode, ExpressionPlanNode value, bool isNot)
    {
        ExecutionPlanNode = executionPlanNode;
        Value = value;
        IsNot = isNot;
    }
    public override bool Execute(RowAccessor row)
    {
        // we need to execute a sub select and get a single value out!
        throw new NotImplementedException();
        //var left = ExpressionPlanNode.Create(inSelect.Left).Execute(row);
        //return inSelect.IsNot ? !EvaluateInSelect(left, inSelect.Expression, row) : EvaluateInSelect(left, inSelect.Expression, row);
    }
}
