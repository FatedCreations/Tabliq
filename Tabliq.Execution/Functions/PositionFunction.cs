using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class PositionFunction : ValueFunction
{
    public PositionFunction()
        : base("POSITION", [new FunctionArgument("expression", typeof(InExpression))])
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {

        var inExpression = arguments.FirstOrDefault();
        if (inExpression is not SubValueInExpressionExecutionPlan subValueExpression)
        {
            throw new ArgumentException("POSITION requires a single IN expression.", nameof(expression));
        }

        var searchFor = subValueExpression.SubValue.ExecuteExpression(accessor)?.ToString();
        var searchIn = subValueExpression.Value.ExecuteExpression(accessor)?.ToString();

        if (searchFor is null || searchIn is null)
        {
            return 0;
        }

        var index = searchIn.IndexOf(searchFor, StringComparison.Ordinal);
        return index >= 0 ? index + 1 : 0;
    }
}
