using System.Net.Security;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ExistsConditionExecutionPlan : ConditionExecutionPlan
{
    public ExecutionPlanNode ExecutionPlanNode { get; }

    public override string Identifier => throw new NotImplementedException("Need to decide on how identity needs to work for execution plans nodes");// $"ExistsCondition({ExecutionPlanNode.Identifier})";

    public ExistsConditionExecutionPlan(ExecutionPlanNode executionPlanNode)
    {
        ExecutionPlanNode = executionPlanNode;
    }
    public override bool Execute(RowAccessor row)
    {
        // we need to execute a sub select and get a single value out!
        throw new NotImplementedException();
        //var left = ExpressionPlanNode.Create(inSelect.Left).Execute(row);
        //return inSelect.IsNot ? !EvaluateInSelect(left, inSelect.Expression, row) : EvaluateInSelect(left, inSelect.Expression, row);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. ExecutionPlanNode.GetExpressions()];
}
