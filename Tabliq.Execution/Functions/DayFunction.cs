using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class DayFunction : ValueFunction
{
    public DayFunction()
        : base("DAY", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {
        var inputValue = arguments.Single().ExecuteExpression(accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Day;
    }
}
