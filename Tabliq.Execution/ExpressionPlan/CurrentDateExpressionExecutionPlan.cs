using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class CurrentDateExpressionExecutionPlan : ExpressionPlanNode
{
    public override string Identifier => "CurrentDate()";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now.Date;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
