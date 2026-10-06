using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public abstract class ValueFunction : SqlFunction
{
    public ValueFunction(
        string name,
        IEnumerable<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, isAggregate: false, arguments, paramsArgument)
    {

    }

    // the computed value of the function, given the arguments
    public abstract object? Execute(FunctionCallExpression expression, RowAccessor accessor);
}
