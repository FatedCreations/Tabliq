using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class SumFunction : AggregateFunction<SumFunction.State>
{
    public SumFunction()
        : base("SUM", [new FunctionArgument("expression")])
    {
    }

    protected override State ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, State? state)
    {
        state ??= new State();
        var val = arguments.ElementAt(0).Execute(accessor);
        state.AddValue(val);
        return state;
    }

    public class State : AggregateFunctionState
    {
        public double Sum { get; set; }
        public bool Any { get; set; }

        public void AddValue(object? value)
        {
            if (value is null)
            {
                // ignore null values for average calculation??
                return;
            }

            var d = Convert.ToDouble(value);
            Sum += d;
            Any = true;
        }

        public override object? GetAggregateValue(CancellationToken cancellationToken)
        {
            if (Any)
            {
                return Sum;
            }

            return null;
        }
    }
}