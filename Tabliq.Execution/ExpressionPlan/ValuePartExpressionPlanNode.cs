using Tabliq.Sql.Ast;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ValuePartExpressionPlanNode : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public string Part { get; set; }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value];
    public override string Identifier => $"ValuePart({Value.Identifier}, {Part})";

    public ValuePartExpressionPlanNode(ExpressionPlanNode value, string part)
    {
        Value = value;
        Part = part;
    }
    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}
