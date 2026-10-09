using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class BinaryOperatorExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Left { get; }
    public BinaryOperator Operator { get; }
    public ExpressionPlanNode Right { get; }

    public override string Identifier => $"BinaryOperator({Left.Identifier} {Operator} {Right.Identifier})";

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];

    public BinaryOperatorExpressionExecutionPlan(ExpressionPlanNode left, BinaryOperator @operator, ExpressionPlanNode right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }
    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var leftValue = Left.ExecuteExpression(row, aggregateValues);
        var rightValue = Right.ExecuteExpression(row, aggregateValues);
        return Operator switch
        {
            BinaryOperator.Add => Convert.ToDouble(leftValue) + Convert.ToDouble(rightValue),
            BinaryOperator.Subtract => Convert.ToDouble(leftValue) - Convert.ToDouble(rightValue),
            BinaryOperator.Multiply => Convert.ToDouble(leftValue) * Convert.ToDouble(rightValue),
            BinaryOperator.Divide => Convert.ToDouble(leftValue) / Convert.ToDouble(rightValue),
            BinaryOperator.Modulus => Convert.ToDouble(leftValue) % Convert.ToDouble(rightValue),
            BinaryOperator.Concatenate => string.Concat(leftValue?.ToString(), rightValue?.ToString()),
            _ => rightValue,
        };
    }
}
