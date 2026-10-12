using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class LiteralExpressionExecutionPlan : ExpressionPlanNode
{
    public LiteralExpressionExecutionPlan(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public override string Identifier => $"Literal({Value})";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => Value;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => Array.Empty<ExpressionPlanNode>();
}
