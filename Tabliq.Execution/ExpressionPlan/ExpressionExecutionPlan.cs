using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.ExpressionPlan;

public abstract class ExpressionPlanNode
{
    public abstract object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null);

    public virtual ExpressionPlanNode TryRewrite(IExecutionProvider? provider, ExecutionRewriteContext? context = null)
        => this;

    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();

    public static ExpressionPlanNode Create(Expression expression)
    {
        if (expression is SelectExpression select)
        {
            return new ScalarSubqueryExpressionExecutionPlan(select);
        }

        return expression switch
        {
            LiteralExpression literal => new LiteralExpressionExecutionPlan(literal.Value),
            IdentifierExpression identifier => new IdentifierExpressionExecutionPlan(identifier),
            StarIdentifierExpression => new ConstantExpressionExecutionPlan(null),
            ParameterIdentifier parameter => new ParameterExpressionExecutionPlan(parameter),
            BracketedExpression bracketed => Create(bracketed.Expression),
            InExpression inExpression => new SubValueInExpressionExecutionPlan(Create(inExpression.Expression), Create(inExpression.SubValue)),
            AsExpression asExpression => new ConvertExpressionExecutionPlan(Create(asExpression.Expression), asExpression.DataType),
            ValueFromExpression valueFromExpression => new ValuePartExpressionPlanNode(Create(valueFromExpression.Expression), valueFromExpression.Part),
            BinaryOperatorExpression binary => new BinaryOperatorExpressionExecutionPlan(Create(binary.Left), binary.Operator, Create(binary.Right)),
            UnaryOperatorExpression unary => new UnaryOperatorExpressionExecutionPlan(Create(unary.Expression), unary.Operator),
            CaseExpression caseExpression => new CaseExpressionExecutionPlan(caseExpression),
            DistinctValueExpression distinctValue => Create(distinctValue.Expression),
            CurrentDate => new CurrentDateExpressionExecutionPlan(),
            CurrentTime => new CurrentTimeExpressionExecutionPlan(),
            CurrentTimestamp => new CurrentTimestampExpressionExecutionPlan(),
            NullValue => new ConstantExpressionExecutionPlan(null),
            FunctionCallExpression functionCall => functionCall.Binding?.GetState<SqlFunction>() switch
            {
                AggregateFunction agg => new AggregateFunctionCallExpressionExecutionPlan(functionCall, agg, functionCall.Arguments.Select(Create)),
                ValueFunction valueFunc => new ValueFunctionCallExpressionExecutionPlan(functionCall, valueFunc, functionCall.Arguments.Select(Create)),
                _ when functionCall.Binding is not null => functionCall.Binding.IsAggregate
                    ? new AggregateFunctionCallExpressionExecutionPlan(
                        functionCall,
                        new PassThroughAggregateFunction(functionCall.FunctionName, functionCall.Arguments.Select(_ => new SqlFunction.FunctionArgument("expression"))),
                        functionCall.Arguments.Select(Create))
                    : new ValueFunctionCallExpressionExecutionPlan(
                        functionCall,
                        new PassThroughValueFunction(functionCall.FunctionName, functionCall.Arguments.Select(_ => new SqlFunction.FunctionArgument("expression"))),
                        functionCall.Arguments.Select(Create)),
                _ => throw new NotSupportedException($"Unsupported function type for function '{functionCall.FunctionName}': {functionCall.Binding?.GetType().Name ?? "null"}"),
            },
            _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

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

public sealed class LiteralExpressionExecutionPlan : ExpressionPlanNode
{
    public LiteralExpressionExecutionPlan(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => Value;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class IdentifierExpressionExecutionPlan : ExpressionPlanNode
{
    public IdentifierExpressionExecutionPlan(IdentifierExpression identifier)
    {
        Identifier = identifier;
    }

    public IdentifierExpression Identifier { get; }

    public TableSymbol? TableSymbol => Identifier.Binding?.TableSymbol;

    public ColumnSymbol? ColumnSymbol => Identifier.Binding?.ColumnSymbol;

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        if (Identifier.Binding is not null)
        {
            var tableName = Identifier.Binding.TableSymbol.TableName;
            var column = Identifier.Binding.ColumnSymbol.Name;
            return GetValue(row, tableName, column) ?? GetValue(row, null, Identifier.Column);
        }

        return GetValue(row, null, Identifier.Column);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class ParameterExpressionExecutionPlan : ExpressionPlanNode
{
    public ParameterExpressionExecutionPlan(ParameterIdentifier parameter)
    {
        Parameter = parameter;
    }

    public ParameterIdentifier Parameter { get; }

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => Parameter.Binding?.ParameterSymbol.State is ExecuterParameter executerParameter ? executerParameter.Value : null;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class CurrentDateExpressionExecutionPlan : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now.Date;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class CurrentTimeExpressionExecutionPlan : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now.TimeOfDay;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class CurrentTimestampExpressionExecutionPlan : ExpressionPlanNode
{
    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => DateTime.Now;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}

public sealed class CaseExpressionExecutionPlan : ExpressionPlanNode
{
    public CaseExpressionExecutionPlan(CaseExpression expression)
    {
        Expression = expression;
    }

    public CaseExpression Expression { get; }

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        throw new NotImplementedException();

        //if (Expression.Expression is not null)
        //{
        //    var switchValue = Create(Expression.Expression).Execute(row, aggregateValues);
        //    foreach (var when in Expression.WhenClauses)
        //    {
        //        var whenCondition = when.Expression is Condition condition
        //            ? Create(condition).Execute(row)
        //            : Equals(switchValue, Create(when.Expression).Execute(row, aggregateValues));
        //        if (whenCondition)
        //        {
        //            return Create(when.Result).Execute(row, aggregateValues);
        //        }
        //    }
        //}
        //else
        //{
        //    foreach (var when in Expression.WhenClauses)
        //    {
        //        if (when.Expression is Condition condition && Create(condition).Execute(row))
        //        {
        //            return Create(when.Result).Execute(row, aggregateValues);
        //        }
        //    }
        //}

        //return Expression.ElseResult is not null ? Create(Expression.ElseResult).Execute(row, aggregateValues) : null;
    }

    public override IEnumerable<ExpressionPlanNode> GetExpressions()
    {
        var expressions = new List<ExpressionPlanNode>();
        if (Expression.Expression is not null)
        {
            expressions.Add(Create(Expression.Expression));
        }

        foreach (var when in Expression.WhenClauses)
        {
            expressions.Add(Create(when.Expression));
            expressions.Add(Create(when.Result));
        }

        if (Expression.ElseResult is not null)
        {
            expressions.Add(Create(Expression.ElseResult));
        }

        return expressions;
    }
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
        if (aggregateValues is not null && aggregateValues.TryGetValue(Expression, out var aggregateValue))
        {
            return aggregateValue;
        }

        return null;
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

public sealed class ScalarSubqueryExpressionExecutionPlan : ExpressionPlanNode
{
    public ScalarSubqueryExpressionExecutionPlan(SelectExpression select)
    {
        Select = select;
    }

    public SelectExpression Select { get; }

    public override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => throw new NotSupportedException("Scalar subqueries must be rewritten to SQL; they are not directly executable in-memory.");

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
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


    public static ConditionExecutionPlan Create(Condition expression)
        => expression switch
        {
            BinaryComparisonCondition comparison => new BinaryComparisonConditionExecutionPlan(ExpressionPlanNode.Create(comparison.Left), comparison.Operator, ExpressionPlanNode.Create(comparison.Right)),
            LogicalCondition logical => new LogicalConditionExecutionPlan(Create(logical.Left), logical.Operator, Create(logical.Right)),
            BracketedCondition bracketed => new BracketedConditionExecutionPlan(Create(bracketed.Expression)),
            UnaryCondition unary => new UnaryConditionExecutionPlan(Create(unary.Right), unary.Operator),
            IsNullCondition isNull => new IsNullConditionExecutionPlan(ExpressionPlanNode.Create(isNull.Expression), isNull.IsNot),
            LikeCondition like => new LikeConditionExecutionPlan(ExpressionPlanNode.Create(like.Left), ExpressionPlanNode.Create(like.Right), like.IsNot),
            BetweenCondition between => new BetweenConditionExecutionPlan(ExpressionPlanNode.Create(between.Left), ExpressionPlanNode.Create(between.From), ExpressionPlanNode.Create(between.To), between.IsNot),
            InListCondition inList => new InListConditionExecutionPlan(ExpressionPlanNode.Create(inList.Left), inList.Items.Select(ExpressionPlanNode.Create), inList.IsNot),
            InSelectCondition inSelect => new InSelectConditionExecutionPlan(ExecutionPlanNode.Create(inSelect.Expression)),
            ExistsCondition exists => new ExistsConditionExecutionPlan(ExecutionPlanNode.Create(exists.SelectExpression)),
            _ => throw new NotSupportedException($"Unsupported condition type: {expression.GetType().Name}")
        };

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