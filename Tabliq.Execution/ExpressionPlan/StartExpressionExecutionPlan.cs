using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class StartExpressionExecutionPlan : ExpressionPlanNode
{
    public StartExpressionExecutionPlan(IReadOnlyList<ColumnBinding> bindings)
    {
        Bindings = bindings;
    }

    public IReadOnlyList<ColumnBinding> Bindings { get; }

    public override string Identifier => $"*";


    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => 1;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
