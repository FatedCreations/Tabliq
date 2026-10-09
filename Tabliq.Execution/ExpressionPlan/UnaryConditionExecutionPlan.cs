using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class UnaryConditionExecutionPlan : ConditionExecutionPlan
{
    public UnaryConditionExecutionPlan(ConditionExecutionPlan unary, UnaryCompararisonOperator @operator)
    {
        Unary = unary;
        Operator = @operator;
    }

    public ConditionExecutionPlan Unary { get; }
    public UnaryCompararisonOperator Operator { get; }

    public override bool Execute(RowAccessor row)
    {
        var value = Unary.Execute(row);

        return Operator == UnaryCompararisonOperator.Not ? !value : value;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. Unary.GetExpressions()];
}
