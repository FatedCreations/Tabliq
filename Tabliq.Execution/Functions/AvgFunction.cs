using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.Functions;

public class AvgFunction : AggregateFunction<AvgFunction.State>
{
    public AvgFunction()
        : base("AVG", [new FunctionArgument("expression")])
    {
    }

    protected override State ProcessRow(FunctionCallExpression expression, RowAccessor accessor, IEnumerable<ExpressionPlanNode> arguments, State? state)
    {
        state ??= new State();
        var val = arguments.Single().ExecuteExpression(accessor);
        state.AddValue(val);
        return state;
    }

    public class State : AggregateFunctionState
    {
        public double Sum { get; set; }
        public int Count { get; set; }

        public void AddValue(object? value)
        {
            if (value is null)
            {
                // ignore null values for average calculation??
                return;
            }

            var d = Convert.ToDouble(value);
            Sum += d;
            Count++;
        }

        public override object? GetAggregateValue()
        {
            if (Count > 0)
            {
                return Sum / Count;
            }

            return null;
        }
    }
}