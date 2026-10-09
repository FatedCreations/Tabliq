using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public abstract class AggregateFunction : SqlFunction
{
    public AggregateFunction(
        string name,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, isAggregate: true, arguments, paramsArgument)
    {
    }

    public abstract AggregateFunctionState InitState();

    public abstract AggregateFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, AggregateFunctionState state);
}

public abstract class AggregateFunction<TState> : AggregateFunction
    where TState : AggregateFunctionState, new()
{
    public AggregateFunction(
        string name,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, arguments, paramsArgument)
    {
    }

    public override AggregateFunctionState InitState()
        => new TState();

    public override AggregateFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, AggregateFunctionState state)
        => ProcessRow(expression, accessor, arguments, (TState)state);

    protected abstract TState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, TState state);
}


public abstract class AggregateFunctionState
{
    public static AggregateFunctionState NullState { get; } = new NoOpState();

    public abstract object? GetAggregateValue();

    public static AggregateFunctionState FromResult(object? result) => new ResultAggregateFunctionState(result);

    private class ResultAggregateFunctionState : AggregateFunctionState
    {
        private readonly object? _result;
        public ResultAggregateFunctionState(object? result)
        {
            _result = result;
        }
        public override object? GetAggregateValue()
        {
            return _result;
        }
    }

    private class NoOpState : AggregateFunctionState
    {
        public override object? GetAggregateValue()
        {
            return null;
        }
    }
}
