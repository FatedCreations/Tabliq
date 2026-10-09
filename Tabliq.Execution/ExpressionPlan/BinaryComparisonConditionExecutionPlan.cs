using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class BinaryComparisonConditionExecutionPlan : ConditionExecutionPlan
{
    public ExpressionPlanNode Left { get; }
    public BinaryCompararisonOperator Operator { get; }
    public ExpressionPlanNode Right { get; }

    public BinaryComparisonConditionExecutionPlan(ExpressionPlanNode left, BinaryCompararisonOperator @operator, ExpressionPlanNode right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];
    public override bool Execute(RowAccessor row)
    {
        var left = Left.ExecuteExpression(row);
        var right = Right.ExecuteExpression(row);
        return Operator switch
        {
            BinaryCompararisonOperator.Equals => ComparisonHelpers.AreEqual(left, right),
            BinaryCompararisonOperator.NotEquals => !ComparisonHelpers.AreEqual(left, right),
            BinaryCompararisonOperator.LessThan => ComparisonHelpers.Compare(left, right) < 0,
            BinaryCompararisonOperator.GreaterThan => ComparisonHelpers.Compare(left, right) > 0,
            BinaryCompararisonOperator.LessThanOrEqual => ComparisonHelpers.Compare(left, right) <= 0,
            BinaryCompararisonOperator.GreaterThanOrEqual => ComparisonHelpers.Compare(left, right) >= 0,
            _ => true,
        };
    }
}
