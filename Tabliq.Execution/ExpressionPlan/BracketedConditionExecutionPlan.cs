namespace Tabliq.Execution.ExpressionPlan;

public sealed class BracketedConditionExecutionPlan : ConditionExecutionPlan
{
    public BracketedConditionExecutionPlan(ConditionExecutionPlan bracketed)
    {
        Bracketed = bracketed;
    }

    public override string Identifier => $"BracketedCondition({Bracketed.Identifier})";

    public ConditionExecutionPlan Bracketed { get; }

    public override bool Execute(RowAccessor row)
        => Bracketed.Execute(row);
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. Bracketed.GetExpressions()];
}
