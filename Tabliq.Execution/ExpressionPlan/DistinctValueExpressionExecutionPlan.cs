using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class DistinctValueExpressionExecutionPlan : ExpressionPlanNode
{
    public DistinctValueExpressionExecutionPlan(Distinctness distinctness, ExpressionPlanNode expression)
    {
        Distinctness = distinctness;
        Expression = expression;
    }

    public override string Identifier => $"DistinctValue({Distinctness}, {Expression.Identifier})";

    public Distinctness Distinctness { get; }
    public ExpressionPlanNode Expression { get; }

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => Expression.ExecuteExpression(row, aggregateValues);

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Expression];
}
