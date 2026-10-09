using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class BracketedExpressionExecutionPlan : ExpressionPlanNode
{
    public BracketedExpressionExecutionPlan(ExpressionPlanNode expression)
    {
        Expression = expression;
    }

    public ExpressionPlanNode Expression { get; }

    public override string Identifier => $"Bracketed({Expression.Identifier})";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => Expression.ExecuteExpression(row, aggregateValues);

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Expression];
}
