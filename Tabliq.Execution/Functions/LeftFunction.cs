using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class LeftFunction : ValueFunction
{
    public LeftFunction()
        : base("LEFT", [new FunctionArgument("string_expression"), new FunctionArgument("integer_expression")])
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {
        var stringValue = arguments.First().ExecuteExpression(accessor)?.ToString();
        var countValue = Convert.ToInt32(arguments.Last().ExecuteExpression(accessor));

        if (stringValue is not string)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        if(stringValue.Length > countValue)
        {
            return stringValue.Substring(0, countValue);
        }

        return stringValue;
    }
}
