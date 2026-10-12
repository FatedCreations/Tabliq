using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Functions;

public static class SqlFunctionArgs
{
    public static SqlFunction.FunctionArgument Arg(string name, Type? requiredType = null, bool optional = false, bool skipBinding = false)
        => new(name, requiredType, skipBinding ? BinderHandling.Skip : BinderHandling.Bind, optional);

    public static SqlFunction.FunctionArgument ParamsArg(string name, Type? requiredType = null, bool optional = false, bool skipBinding = false)
        => new(name, requiredType, skipBinding ? BinderHandling.Skip : BinderHandling.Bind, optional);
}

public class PassThroughValueFunction : ValueFunction
{
    public PassThroughValueFunction(string name)
        : base(name, Enumerable.Empty<SqlFunction.FunctionArgument>())
    {
    }

    public PassThroughValueFunction(string name, params SqlFunction.FunctionArgument[] arguments)
        : base(name, arguments)
    {
    }

    public PassThroughValueFunction(string name, IEnumerable<SqlFunction.FunctionArgument> arguments, SqlFunction.FunctionArgument? paramsArgument = null)
        : base(name, arguments, paramsArgument)
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments)
        => throw new NotSupportedException($"Function '{Name}' must be executed by the SQL provider.");
}

public class PassThroughAggregateFunction : AggregateFunction<PassThroughAggregateFunction.State>
{
    public PassThroughAggregateFunction(string name)
        : base(name, Array.Empty<SqlFunction.FunctionArgument>())
    {
    }

    public PassThroughAggregateFunction(string name, params SqlFunction.FunctionArgument[] arguments)
        : base(name, arguments.ToList())
    {
    }

    public PassThroughAggregateFunction(string name, IEnumerable<SqlFunction.FunctionArgument> arguments, SqlFunction.FunctionArgument? paramsArgument = null)
        : base(name, arguments.ToList(), paramsArgument)
    {
    }

    protected override State ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, State state)
    {
        state.LastValue = arguments.FirstOrDefault()?.ExecuteExpression(accessor);
        return state;
    }

    public class State : AggregateFunctionState
    {
        public object? LastValue { get; set; }

        public override object? GetAggregateValue()
            => LastValue;
    }
}
