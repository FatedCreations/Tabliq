using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class LogicalConditionExecutionPlan : ConditionExecutionPlan
{
    public LogicalConditionExecutionPlan(ConditionExecutionPlan left, LogicalOperator @operator, ConditionExecutionPlan right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public ConditionExecutionPlan Left { get; }

    public LogicalOperator Operator { get; }

    public ConditionExecutionPlan Right { get; }

    public override bool Execute(RowAccessor row)
        => Operator switch
        {
            LogicalOperator.And => Left.Execute(row) && Right.Execute(row),
            LogicalOperator.Or => Left.Execute(row) || Right.Execute(row),
            _ => true,
        };
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [..Left.GetExpressions(), ..Right.GetExpressions()];
}
