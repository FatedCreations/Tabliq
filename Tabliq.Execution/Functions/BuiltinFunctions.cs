using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Execution.Functions;

public static class BuiltinFunctions
{
    public static IEnumerable<SqlFunction> BuiltingFunctions { get; } = [
            new CountFunction()
       ];
}

public abstract class SqlFunction
{
    internal SqlFunction(
        string name,
        bool isAggregate,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
    {
        Name = name;
        IsAggregate = isAggregate;
        Arguments = arguments;
        ParamsArgument = paramsArgument;

        FunctionSymbol = new FunctionSymbol(
            name,
            isAggregate,
            arguments.Select(a => new FunctionArgumentSymbol(
                a.Name,
                a.RequiredType,
                a.BinderHandling,
                a.Optional)).ToList(),
            paramsArgument != null ? new FunctionArgumentSymbol(
                paramsArgument.Name,
                paramsArgument.RequiredType,
                paramsArgument.BinderHandling,
                paramsArgument.Optional) : null)
        {
            State = this
        };
    }

    public string Name { get; }
    public bool IsAggregate { get; }
    public IReadOnlyList<FunctionArgument> Arguments { get; }
    public FunctionArgument? ParamsArgument { get; }

    public FunctionSymbol FunctionSymbol { get; }

    public sealed record FunctionArgument(string Name, Type? RequiredType = null, BinderHandling BinderHandling = BinderHandling.Bind, bool Optional = false);
}

public abstract class ValueFunction : SqlFunction
{
    public ValueFunction(
        string name,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, isAggregate: false, arguments, paramsArgument)
    {

    }

    // the computed value of the function, given the arguments
    public abstract object? Execute(FunctionCallExpression expression, RowAccessor accessor);
}

public abstract class AggregateFunction : SqlFunction
{
    public AggregateFunction(
        string name,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, isAggregate: true, arguments, paramsArgument)
    {
    }

    public abstract AggregateFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, AggregateFunctionState? state);
}

public abstract class AggregateFunction<TState> : AggregateFunction
    where TState : AggregateFunctionState
{
    public AggregateFunction(
        string name,
        IReadOnlyList<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
        : base(name, arguments, paramsArgument)
    {
    }

    public override AggregateFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, AggregateFunctionState? state)
    {
        TState? typedState = state is null ? default : (TState?)state;
        return ProcessRow(expression, accessor, typedState);
    }

    protected abstract TState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, TState? state);
}

public abstract class AggregateFunctionState
{
    public abstract object? GetAggregateValue(CancellationToken cancellationToken);
    
    public static AggregateFunctionState FromResult(object? result) => new ResultAggregateFunctionState(result);

    private class ResultAggregateFunctionState : AggregateFunctionState
    {
        private readonly object? _result;
        public ResultAggregateFunctionState(object? result)
        {
            _result = result;
        }
        public override object? GetAggregateValue(CancellationToken cancellationToken)
        {
            return _result;
        }
    }   
}

public class CountFunction : AggregateFunction<CountFunction.CountFunctionState>
{
    public CountFunction()
        : base("COUNT", new List<FunctionArgument> { new FunctionArgument("x") })
    {
    }

    protected override CountFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, CountFunctionState? state)
    {
        state??= new CountFunctionState();
        state.Increment();
        return state;
    }

    public class CountFunctionState : AggregateFunctionState
    {
        private int _count;
        public CountFunctionState()
        {
            _count = 0;
        }
        public void Increment()
        {
            _count++;
        }
        public override object? GetAggregateValue(CancellationToken cancellationToken)
        {
            return _count;
        }
    }
}