using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class CurrentTimestampExpressionExecutionPlan : ExpressionPlanNode
{
    public override string Identifier => "CurrentTimestamp()";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
