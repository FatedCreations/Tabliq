using System.Linq.Expressions;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tabliq.Execution.ExpressionPlan;

public abstract class ExpressionPlanNode
{
    public abstract object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null);

    public virtual ExpressionPlanNode TryRewrite(IExecutionProvider? provider, ExecutionRewriteContext? context = null)
        => this;

    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();

    public static object? NormalizeValue(object? value)
        => value switch
        {
            double doubleValue => Convert.ToInt32(doubleValue),
            decimal decimalValue => Convert.ToInt32(decimalValue),
            float floatValue => Convert.ToInt32(floatValue),
            string stringValue when double.TryParse(stringValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedDouble) => Convert.ToInt32(parsedDouble),
            _ => value,
        };

    public static object? GetValue(RowAccessor row, string? tableName, string columnName)
    {
        if (tableName is not null)
        {
            var composite = $"{tableName}.{columnName}";
            if (row.TryGetValue(composite, out var compositeValue))
            {
                return compositeValue;
            }

            string? match = null;
            var suffixKey = $".{columnName}";
            foreach (var c in row.Columns)
            {
                if (c.Equals(suffixKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (match is not null)
                    {
                        match = null; // we have multiple can't use any!
                        break;
                    }
                    match = c;
                }
            }

            if (match is not null)
            {
                return row[match];
            }

            if (row.TryGetValue(columnName, out var directByName))
            {
                return directByName;
            }
        }

        if (row.TryGetValue(columnName, out var direct))
        {
            return direct;
        }

        var suffixKeyFallback = $".{columnName}";
        foreach (var c in row.Columns)
        {
            if (c.Equals(suffixKeyFallback, StringComparison.OrdinalIgnoreCase))
            {
                return row[c];
            }
        }

        return null;
    }

}

public sealed class LiteralExpressionExecutionPlan(object? value) : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => value;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class IdentifierExpressionExecutionPlan(IdentifierExpression identifier) : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        if (identifier.Binding is not null)
        {
            var tableName = identifier.Binding.TableSymbol.TableName;
            var column = identifier.Binding.ColumnSymbol.Name;
            return GetValue(row, tableName, column) ?? GetValue(row, null, identifier.Column);
        }

        return GetValue(row, null, identifier.Column);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class ConvertExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public DataType DataType { get; set; }

    public ConvertExpressionExecutionPlan(ExpressionPlanNode value, DataType dataType)
    {
        Value = value;
        DataType = dataType;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value];
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}
public sealed class SubValueInExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public ExpressionPlanNode SubValue { get; set; }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value, SubValue];

    public SubValueInExpressionExecutionPlan(ExpressionPlanNode value, ExpressionPlanNode subValue)
    {
        Value = value;
        SubValue = subValue;
    }
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}
public sealed class ValuePartExpressionPlanNode : ExpressionPlanNode
{
    public ExpressionPlanNode Value { get; }
    public string Part { get; set; }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value];

    public ValuePartExpressionPlanNode(ExpressionPlanNode value, string part)
    {
        Value = value;
        Part = part;
    }
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new Exception("Cannot be executed directly, requires a SqlFunction to handle this as a special case");
    }
}

public sealed class BinaryOperatorExpressionExecutionPlan : ExpressionPlanNode
{
    public ExpressionPlanNode Left { get; }
    public BinaryOperator Operator { get; }
    public ExpressionPlanNode Right { get; }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];

    public BinaryOperatorExpressionExecutionPlan(ExpressionPlanNode left, BinaryOperator @operator, ExpressionPlanNode right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var leftValue = Left.Execute(row, aggregateValues);
        var rightValue = Right.Execute(row, aggregateValues);
        return Operator switch
        {
            BinaryOperator.Add => Convert.ToDouble(leftValue) + Convert.ToDouble(rightValue),
            BinaryOperator.Subtract => Convert.ToDouble(leftValue) - Convert.ToDouble(rightValue),
            BinaryOperator.Multiply => Convert.ToDouble(leftValue) * Convert.ToDouble(rightValue),
            BinaryOperator.Divide => Convert.ToDouble(leftValue) / Convert.ToDouble(rightValue),
            BinaryOperator.Modulus => Convert.ToDouble(leftValue) % Convert.ToDouble(rightValue),
            BinaryOperator.Concatenate => string.Concat(leftValue?.ToString(), rightValue?.ToString()),
            _ => rightValue,
        };
    }
}

public sealed class UnaryOperatorExpressionExecutionPlan: ExpressionPlanNode
{
    public ExpressionPlanNode Inner { get; }
    public UnaryOperator Operator { get; }

    public UnaryOperatorExpressionExecutionPlan(ExpressionPlanNode inner, UnaryOperator @operator)
    {
        Inner = inner;
        Operator = @operator;
    }
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var value = Inner.Execute(row, aggregateValues);
        return Operator == UnaryOperator.Negate ? -Convert.ToDouble(value) : value;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Inner];
}

//public sealed class CaseExpressionExecutionPlan(CaseExpression caseExpression) : ExpressionPlanNode
//{
//    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
//    {
//        if (caseExpression.Expression is not null)
//        {
//            var switchValue = Create(caseExpression.Expression).Execute(row, aggregateValues);
//            foreach (var when in caseExpression.WhenClauses)
//            {
//                if (when.Expression is Condition condition && ConditionExecutionPlan.Create(condition).Execute(row))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }

//                if (Equals(switchValue, Create(when.Expression).Execute(row, aggregateValues)))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }
//            }
//        }
//        else
//        {
//            foreach (var when in caseExpression.WhenClauses)
//            {
//                if (when.Expression is Condition condition && ConditionExecutionPlan.Create(condition).Execute(row))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }
//            }
//        }

//        return caseExpression.ElseResult is not null ? Create(caseExpression.ElseResult).Execute(row, aggregateValues) : null;
//    }
//}

public sealed class AggregateFunctionCallExpressionExecutionPlan : ExpressionPlanNode
{
    public AggregateFunction Function { get; }
    public IReadOnlyList<ExpressionPlanNode> Arguments { get; }
    public FunctionCallExpression Expression { get; }  // todo remnove the need for this, we should be able to get the expression from the function call itself
    public AggregateFunctionCallExpressionExecutionPlan(FunctionCallExpression expression, AggregateFunction function, IEnumerable<ExpressionPlanNode> arguments)
    {
        Function = function;
        Arguments = arguments.ToList();
        Expression = expression;
    }
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new InvalidOperationException("Unsupported function call");
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => Arguments;
}

public sealed class ValueFunctionCallExpressionExecutionPlan : ExpressionPlanNode
{
    public ValueFunction Function { get; }
    public IReadOnlyList<ExpressionPlanNode> Arguments { get; }
    public FunctionCallExpression Expression { get; }  // todo remnove the need for this, we should be able to get the expression from the function call itself
    public ValueFunctionCallExpressionExecutionPlan(FunctionCallExpression expression, ValueFunction function, IEnumerable<ExpressionPlanNode> arguments)
    {
        Function = function;
        Arguments = arguments.ToList();
        Expression = expression;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => Arguments;

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        return Function.Execute(Expression, row, Arguments);
    }
}

public sealed class ConstantExpressionExecutionPlan(object? value) : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => value;
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public abstract class ConditionExecutionPlan
{
    public abstract bool Execute(RowAccessor row);

    public virtual ConditionExecutionPlan TryRewrite(IExecutionProvider? provider, ExecutionRewriteContext? context = null)
        => this;
    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();
}

public sealed class BinaryComparisonConditionExecutionPlan : ConditionExecutionPlan
{
    public ExpressionPlanNode Left { get; }
    public BinaryCompararisonOperator Operator { get; }
    public ExpressionPlanNode Right { get; }

    public BinaryComparisonConditionExecutionPlan(ExpressionPlanNode left, BinaryCompararisonOperator @operator, ExpressionPlanNode right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];
    public override bool Execute(RowAccessor row)
    {
        var left = Left.Execute(row);
        var right = Right.Execute(row);
        return Operator switch
        {
            BinaryCompararisonOperator.Equals => ComparisonHelpers.AreEqual(left, right),
            BinaryCompararisonOperator.NotEquals => !ComparisonHelpers.AreEqual(left, right),
            BinaryCompararisonOperator.LessThan => ComparisonHelpers.Compare(left, right) < 0,
            BinaryCompararisonOperator.GreaterThan => ComparisonHelpers.Compare(left, right) > 0,
            BinaryCompararisonOperator.LessThanOrEqual => ComparisonHelpers.Compare(left, right) <= 0,
            BinaryCompararisonOperator.GreaterThanOrEqual => ComparisonHelpers.Compare(left, right) >= 0,
            _ => true,
        };
    }
}

public sealed class LogicalConditionExecutionPlan : ConditionExecutionPlan
{
    public LogicalConditionExecutionPlan(ConditionExecutionPlan left, LogicalOperator @operator, ConditionExecutionPlan right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public ConditionExecutionPlan Left { get; }

    public LogicalOperator Operator { get; }

    public ConditionExecutionPlan Right { get; }

    public override bool Execute(RowAccessor row)
        => Operator switch
        {
            LogicalOperator.And => Left.Execute(row) && Right.Execute(row),
            LogicalOperator.Or => Left.Execute(row) || Right.Execute(row),
            _ => true,
        };
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [..Left.GetExpressions(), ..Right.GetExpressions()];
}

public sealed class BracketedConditionExecutionPlan : ConditionExecutionPlan
{
    public BracketedConditionExecutionPlan(ConditionExecutionPlan bracketed)
    {
        Bracketed = bracketed;
    }

    public ConditionExecutionPlan Bracketed { get; }

    public override bool Execute(RowAccessor row)
        => Bracketed.Execute(row);
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. Bracketed.GetExpressions()];
}

public sealed class UnaryConditionExecutionPlan : ConditionExecutionPlan
{
    public UnaryConditionExecutionPlan(ConditionExecutionPlan unary, UnaryCompararisonOperator @operator)
    {
        Unary = unary;
        Operator = @operator;
    }

    public ConditionExecutionPlan Unary { get; }
    public UnaryCompararisonOperator Operator { get; }

    public override bool Execute(RowAccessor row)
    {
        var value = Unary.Execute(row);

        return Operator == UnaryCompararisonOperator.Not ? !value : value;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. Unary.GetExpressions()];
}

public sealed class IsNullConditionExecutionPlan: ConditionExecutionPlan
{
    public IsNullConditionExecutionPlan(ExpressionPlanNode expression, bool isNot)
    {
        Expression = expression;
        IsNot = isNot;
    }

    public ExpressionPlanNode Expression { get; }
    public bool IsNot { get; }

    public override bool Execute(RowAccessor row)
        => Expression.Execute(row) is null == !IsNot;
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Expression];
}

public sealed class LikeConditionExecutionPlan : ConditionExecutionPlan
{
    public LikeConditionExecutionPlan(ExpressionPlanNode left, ExpressionPlanNode right, bool isNot)
    {
        Left = left;
        Right = right;
        IsNot = isNot;
    }

    public ExpressionPlanNode Left { get; }
    public ExpressionPlanNode Right { get; }
    public bool IsNot { get; }

    public override bool Execute(RowAccessor row)
    {
        var left = Left.Execute(row)?.ToString();
        var right = Right.Execute(row)?.ToString();
        var result = MatchesLike(left, right);
        return IsNot ? !result : result;
    }
    private static bool MatchesLike(string? left, string? pattern)
    {
        if (left is null || pattern is null)
        {
            return left is null && pattern is null;
        }

        var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("%", ".*")
            .Replace("_", ".");

        return System.Text.RegularExpressions.Regex.IsMatch(left, $"^{regexPattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];
}

public sealed class BetweenConditionExecutionPlan : ConditionExecutionPlan
{
    public BetweenConditionExecutionPlan(ExpressionPlanNode value, ExpressionPlanNode from, ExpressionPlanNode to, bool isNot)
    {
        Value = value;
        From = from;
        To = to;
        IsNot = isNot;
    }

    public ExpressionPlanNode Value { get; }
    public ExpressionPlanNode From { get; }
    public ExpressionPlanNode To { get; }
    public bool IsNot { get; }


    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Value, From, To];

    public override bool Execute(RowAccessor row)
    {
        var value = Value.Execute(row);
        var from = From.Execute(row);
        var to = To.Execute(row);

        var result = ComparisonHelpers.Compare(value, from) >= 0 && ComparisonHelpers.Compare(value, to) <= 0;
        return IsNot ? !result : result;
    }
}

public sealed class InListConditionExecutionPlan: ConditionExecutionPlan
{
    public InListConditionExecutionPlan(ExpressionPlanNode left, IEnumerable<ExpressionPlanNode> items, bool isNot)
    {
        Left = left;
        Items = items;
        IsNot = isNot;
    }

    public ExpressionPlanNode Left { get; }
    public IEnumerable<ExpressionPlanNode> Items { get; }
    public bool IsNot { get; }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, ..Items];
    public override bool Execute(RowAccessor row)
    {
        var left = Left.Execute(row);
        var result = false;
        foreach (var item in Items)
        {
            if (Equals(left, item.Execute(row)))
            {
                result = true;
                break;
            }
        }

        return IsNot ? !result : result;
    }
}

public sealed class InSelectConditionExecutionPlan : ConditionExecutionPlan
{
    public ExecutionPlanNode ExecutionPlanNode { get; }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. ExecutionPlanNode.GetExpressions()];
    public InSelectConditionExecutionPlan(ExecutionPlanNode executionPlanNode)
    {
        ExecutionPlanNode = executionPlanNode;
    }
    public override bool Execute(RowAccessor row)
    {
        // we need to execute a sub select and get a single value out!
        throw new NotImplementedException();
        //var left = ExpressionPlanNode.Create(inSelect.Left).Execute(row);
        //return inSelect.IsNot ? !EvaluateInSelect(left, inSelect.Expression, row) : EvaluateInSelect(left, inSelect.Expression, row);
    }
}

public sealed class ExistsConditionExecutionPlan : ConditionExecutionPlan
{
    public ExecutionPlanNode ExecutionPlanNode { get; }

    public ExistsConditionExecutionPlan(ExecutionPlanNode executionPlanNode)
    {
        ExecutionPlanNode = executionPlanNode;
    }
    public override bool Execute(RowAccessor row)
    {
        // we need to execute a sub select and get a single value out!
        throw new NotImplementedException();
        //var left = ExpressionPlanNode.Create(inSelect.Left).Execute(row);
        //return inSelect.IsNot ? !EvaluateInSelect(left, inSelect.Expression, row) : EvaluateInSelect(left, inSelect.Expression, row);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. ExecutionPlanNode.GetExpressions()];
}

public sealed class ConstantConditionExecutionPlan(bool value) : ConditionExecutionPlan
{
    public override bool Execute(RowAccessor row) => value;
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public static class ComparisonHelpers
{
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            return Convert.ToDecimal(left, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDecimal(right, System.Globalization.CultureInfo.InvariantCulture);
        }

        return left.Equals(right);
    }

    public static int Compare(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftDecimal = Convert.ToDecimal(left, System.Globalization.CultureInfo.InvariantCulture);
            var rightDecimal = Convert.ToDecimal(right, System.Globalization.CultureInfo.InvariantCulture);
            return leftDecimal.CompareTo(rightDecimal);
        }

        return Comparer<object?>.Default.Compare(left, right);
    }

    public static bool IsNumeric(object value)
        => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}