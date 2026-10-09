using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class CurrentTimeExpressionExecutionPlan : ExpressionPlanNode
{
    public override string Identifier => "CurrentTime()";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now.TimeOfDay;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
