using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Functions;

public class MonthFunction : ValueFunction
{
    public MonthFunction()
        : base("MONTH", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
    {
        var inputValue = arguments.Single().Execute(accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Month - 1; //sql starts months at 0, but dotnet starts a 1;
    }
}
