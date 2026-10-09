using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ScalarSubqueryExpressionExecutionPlan : ExpressionPlanNode
{
    public ScalarSubqueryExpressionExecutionPlan(ExecutionPlanNode select)
    {
        Select = select;
    }   

    public ExecutionPlanNode Select { get; }

    public override string Identifier => $"ScalarSubquery({Select})";

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => throw new NotSupportedException("Scalar subqueries must be rewritten to SQL; they are not directly executable in-memory.");

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
