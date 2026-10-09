using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public abstract class ExpressionPlanNode
{
    // a deterministic identifier for this expression plan node, used for caching and comparison purposes
    public abstract string Identifier { get; }

    public object? ExecuteExpression(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        if (row.TryGetValue(Identifier, out var value))
        {
            return value;
        }
        return Execute(row, aggregateValues);
    }

    protected abstract object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null);

    public virtual ExpressionPlanNode TryRewrite(ExecutionRewriteContext? context = null)
        => this;

    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();

    public static ExpressionPlanNode Create(Expression expression)
    {
        if (expression is SelectExpression select)
        {
            return new ScalarSubqueryExpressionExecutionPlan(ExecutionPlanNode.Create(select));
        }

        return expression switch
        {
            LiteralExpression literal => new LiteralExpressionExecutionPlan(literal.Value),
            IdentifierExpression identifier => new IdentifierExpressionExecutionPlan(identifier),
            StarIdentifierExpression => new StartExpressionExecutionPlan(), // count(1) ?? 
            ParameterIdentifier parameter => new ParameterExpressionExecutionPlan(parameter),
            BracketedExpression bracketed => new BracketedExpressionExecutionPlan(Create(bracketed.Expression)),
            InExpression inExpression => new SubValueInExpressionExecutionPlan(Create(inExpression.Expression), Create(inExpression.SubValue)),
            AsExpression asExpression => new ConvertExpressionExecutionPlan(Create(asExpression.Expression), asExpression.DataType),
            ValueFromExpression valueFromExpression => new ValuePartExpressionPlanNode(Create(valueFromExpression.Expression), valueFromExpression.Part),
            BinaryOperatorExpression binary => new BinaryOperatorExpressionExecutionPlan(Create(binary.Left), binary.Operator, Create(binary.Right)),
            UnaryOperatorExpression unary => new UnaryOperatorExpressionExecutionPlan(Create(unary.Expression), unary.Operator),
            // CaseExpression caseExpression => new CaseExpressionExecutionPlan(caseExpression),
            DistinctValueExpression distinctValue => new DistinctValueExpressionExecutionPlan(distinctValue.Distinctness, Create(distinctValue.Expression)),
            CurrentDate => new CurrentDateExpressionExecutionPlan(),
            CurrentTime => new CurrentTimeExpressionExecutionPlan(),
            CurrentTimestamp => new CurrentTimestampExpressionExecutionPlan(),
            NullValue => new LiteralExpressionExecutionPlan(null),
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
