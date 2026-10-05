using System.Text.RegularExpressions;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public static class EvaluationHelpers
{
    internal static bool EvaluateCondition(Condition condition, RowAccessor row)
    {
        switch (condition)
        {
            case BinaryComparisonCondition comparison:
                var left = EvaluateExpression(comparison.Left, row);
                var right = EvaluateExpression(comparison.Right, row);
                return comparison.Operator switch
                {
                    BinaryCompararisonOperator.Equals => Equals(left, right),
                    BinaryCompararisonOperator.NotEquals => !Equals(left, right),
                    BinaryCompararisonOperator.LessThan => Compare(left, right) < 0,
                    BinaryCompararisonOperator.GreaterThan => Compare(left, right) > 0,
                    BinaryCompararisonOperator.LessThanOrEqual => Compare(left, right) <= 0,
                    BinaryCompararisonOperator.GreaterThanOrEqual => Compare(left, right) >= 0,
                    _ => true,
                };
            case LogicalCondition logical:
                return logical.Operator switch
                {
                    LogicalOperator.And => EvaluateCondition(logical.Left, row) && EvaluateCondition(logical.Right, row),
                    LogicalOperator.Or => EvaluateCondition(logical.Left, row) || EvaluateCondition(logical.Right, row),
                    _ => true,
                };
            case BracketedCondition bracketed:
                return EvaluateExpression(bracketed.Expression, row) is not null;
            case UnaryCondition unary:
                return unary.Operator == UnaryCompararisonOperator.Not ? EvaluateExpression(unary.Right, row) is null : true;
            case IsNullCondition isNull:
                return EvaluateExpression(isNull.Expression, row) is null;
            default:
                return true;
        }
    }

    internal static object? EvaluateExpression(Expression expression, RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        switch (expression)
        {
            case LiteralExpression literal:
                return literal.Value;
            case IdentifierExpression identifier:
                return ResolveIdentifierValue(row, identifier);
            case BinaryOperatorExpression binary:
                var left = EvaluateExpression(binary.Left, row, aggregateValues);
                var right = EvaluateExpression(binary.Right, row, aggregateValues);
                return binary.Operator switch
                {
                    BinaryOperator.Add => Convert.ToDouble(left) + Convert.ToDouble(right),
                    BinaryOperator.Subtract => Convert.ToDouble(left) - Convert.ToDouble(right),
                    BinaryOperator.Multiply => Convert.ToDouble(left) * Convert.ToDouble(right),
                    BinaryOperator.Divide => Convert.ToDouble(left) / Convert.ToDouble(right),
                    BinaryOperator.Modulus => Convert.ToDouble(left) % Convert.ToDouble(right),
                    _ => right,
                };
            case UnaryOperatorExpression unary:
                var value = EvaluateExpression(unary.Expression, row, aggregateValues);
                return unary.Operator == UnaryOperator.Negate ? -Convert.ToDouble(value) : value;
            case FunctionCallExpression functionCall:
                if (aggregateValues?.TryGetValue(functionCall, out var aggregateValue) == true)
                {
                    return aggregateValue;
                }
                if (functionCall.Binding?.State is ValueFunction valueFunction)
                {
                    return valueFunction.Execute(functionCall, row); // are agg values needed?
                }

                throw new Exception("Unsupported function call");
            default:
                return null;
        }
    }

    private static object? ResolveIdentifierValue(RowAccessor row, IdentifierExpression identifier)
    {
        if (identifier.Binding is not null)
        {
            var tableName = identifier.Binding.TableSymbol.TableName;
            var column = identifier.Binding.ColumnSymbol.Name;
            return GetValue(row, tableName, column) ?? GetValue(row, null, identifier.Column);
        }

        return GetValue(row, null, identifier.Column);
    }

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
    public static object? NormalizeValue(object? value)
    {
        if (value is double doubleValue)
        {
            return Convert.ToInt32(doubleValue);
        }

        if (value is decimal decimalValue)
        {
            return Convert.ToInt32(decimalValue);
        }

        if (value is float floatValue)
        {
            return Convert.ToInt32(floatValue);
        }

        return value;
    }

    private static int Compare(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;
        return Comparer<object?>.Default.Compare(left, right);
    }
}
