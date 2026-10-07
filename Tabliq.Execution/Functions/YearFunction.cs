using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class YearFunction : ValueFunction
{
    public YearFunction()
        : base("YEAR", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        var inputValue = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Year;
    }
}
