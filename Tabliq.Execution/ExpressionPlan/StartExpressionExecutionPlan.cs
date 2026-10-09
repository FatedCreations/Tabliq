using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class StartExpressionExecutionPlan : ExpressionPlanNode
{
    public StartExpressionExecutionPlan()
    {
    }

    public override string Identifier => $"*";


    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => 1;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
