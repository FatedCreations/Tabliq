using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class YearFunction : ValueFunction
{
    public YearFunction()
        : base("YEAR", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {
        var inputValue = arguments.ElementAt(0).ExecuteExpression(accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Year;
    }
}
