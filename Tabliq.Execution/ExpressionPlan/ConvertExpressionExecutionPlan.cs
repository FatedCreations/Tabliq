using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ConvertExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public DataType DataType { get; set; }

    public override string Identifier => $"Convert({Value.Identifier} AS {DataType})";

    public ConvertExpressionExecutionPlan(ExpressionPlanNode value, DataType dataType)
    {
        Value = value;
        DataType = dataType;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value];

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}
