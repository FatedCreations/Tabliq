using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class CountFunction : AggregateFunction<CountFunction.CountFunctionState>
{
    public CountFunction()
        : base("COUNT", new List<FunctionArgument> { new FunctionArgument("x") })
    {
    }

    protected override CountFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, CountFunctionState state)
    {
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