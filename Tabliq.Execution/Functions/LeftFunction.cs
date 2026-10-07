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
        var stringValue = arguments.First().Execute(accessor)?.ToString();
        var countValue = Convert.ToInt32(arguments.Last().Execute(accessor));

        if (stringValue is not string)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return stringValue.Substring(0, Math.Max(countValue, stringValue.Length));
    }
}
