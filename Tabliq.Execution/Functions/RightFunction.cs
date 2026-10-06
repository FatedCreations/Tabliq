using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class RightFunction : ValueFunction
{
    public RightFunction()
        : base("RIGHT", [new FunctionArgument("string_expression"), new FunctionArgument("integer_expression")])
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        var stringValue = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor)?.ToString();
        var countValue = Convert.ToInt32(EvaluationHelpers.EvaluateExpression(expression.Arguments[1], accessor));

        if (stringValue is not string)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        var start = stringValue.Length - countValue;

        return stringValue.Substring(Math.Min(start, 0));
    }
}
