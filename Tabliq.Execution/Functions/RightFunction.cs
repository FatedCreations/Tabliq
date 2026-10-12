using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class RightFunction : ValueFunction
{
    public RightFunction()
        : base("RIGHT", [new FunctionArgument("string_expression"), new FunctionArgument("integer_expression")])
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {
        var stringValue = arguments.ElementAt(0).ExecuteExpression(accessor)?.ToString();
        var countValue = Convert.ToInt32(arguments.ElementAt(1).ExecuteExpression(accessor));

        if (stringValue is not string)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        var start = stringValue.Length - countValue;

        return stringValue.Substring(Math.Min(start, 0));
    }
}
