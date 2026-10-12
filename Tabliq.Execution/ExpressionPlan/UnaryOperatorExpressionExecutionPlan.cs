using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class UnaryOperatorExpressionExecutionPlan: ExpressionPlanNode
{
    public ExpressionPlanNode Inner { get; }
    public UnaryOperator Operator { get; }

    public override string Identifier => $"UnaryOperator({Operator}, {Inner.Identifier})";

    public UnaryOperatorExpressionExecutionPlan(ExpressionPlanNode inner, UnaryOperator @operator)
    {
        Inner = inner;
        Operator = @operator;
    }
    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var value = Inner.ExecuteExpression(row, aggregateValues);
        return Operator == UnaryOperator.Negate ? -Convert.ToDouble(value) : value;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Inner];
}
