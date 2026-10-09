using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class SubValueInExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public ExpressionPlanNode SubValue { get; set; }

    public override string Identifier => $"SubValueIn({Value.Identifier}, {SubValue.Identifier})";

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value, SubValue];

    public SubValueInExpressionExecutionPlan(ExpressionPlanNode value, ExpressionPlanNode subValue)
    {
        Value = value;
        SubValue = subValue;
    }
    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}
