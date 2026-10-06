using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class PositionFunction : ValueFunction
{
    public PositionFunction()
        : base("POSITION", [new FunctionArgument("expression", typeof(InExpression))])
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        if (expression.Arguments.Count != 1 || expression.Arguments[0] is not InExpression inExpression)
        {
            throw new ArgumentException("POSITION requires a single IN expression.", nameof(expression));
        }

        var searchFor = EvaluationHelpers.EvaluateExpression(inExpression.SubValue, accessor)?.ToString();
        var searchIn = EvaluationHelpers.EvaluateExpression(inExpression.Expression, accessor)?.ToString();

        if (searchFor is null || searchIn is null)
        {
            return 0;
        }

        var index = searchIn.IndexOf(searchFor, StringComparison.Ordinal);
        return index >= 0 ? index + 1 : 0;
    }
}
