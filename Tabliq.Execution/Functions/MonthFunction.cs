using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Functions;

public class MonthFunction : ValueFunction
{
    public MonthFunction()
        : base("MONTH", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        var inputValue = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Month - 1; //sql starts months at 0, but dotnet starts a 1;
    }
}

public class YearFunction : ValueFunction
{
    public YearFunction()
        : base("YEAR", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        var inputValue = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Year;
    }
}

public class DayFunction : ValueFunction
{
    public DayFunction()
        : base("DAY", new List<FunctionArgument> { new FunctionArgument("expression") })
    {
    }

    public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
    {
        var inputValue = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);

        if (inputValue is not DateTime dt)
        {
            throw new Exception("Input value must be a DateTime.");
        }

        return dt.Day;
    }
}
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
public class LeftFunction : ValueFunction
{
    public LeftFunction()
        : base("LEFT", [new FunctionArgument("string_expression"), new FunctionArgument("integer_expression")])
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

        return stringValue.Substring(0, Math.Max(countValue, stringValue.Length));
    }
}
public class AvgFunction : AggregateFunction<AvgFunction.State>
{
    public AvgFunction()
        : base("AVG", [new FunctionArgument("expression")])
    {
    }

    protected override State ProcessRow(FunctionCallExpression expression, RowAccessor accessor, State? state)
    {
        state ??= new State();
        var val = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);
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

        public override object? GetAggregateValue(CancellationToken cancellationToken)
        {
            if (Count > 0)
            {
                return Sum / Count;
            }

            return null;
        }
    }
}