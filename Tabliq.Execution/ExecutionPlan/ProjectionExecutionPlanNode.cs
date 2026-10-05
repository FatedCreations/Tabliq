using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public sealed class ProjectionExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly IReadOnlyList<SelectProjection> _projections;

    public SelectExpression? SourceSelect { get; }

    public ExecutionPlanNode Input => _input;

    public ProjectionExecutionPlanNode(ExecutionPlanNode input, IReadOnlyList<SelectProjection> projections, SelectExpression? sourceSelect = null)
    {
        _input = input;
        _projections = projections;
        SourceSelect = sourceSelect;
    }
    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite()
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite() ?? _input;
        if (newInput != _input)
        {
            currentNode = new ProjectionExecutionPlanNode(newInput, _projections, SourceSelect);
        }

        currentNode = newInput?.Provider?.TryRewrite(currentNode) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var reader = await _input.ExecuteAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var fields = reader.GetFields();
            var values = reader.GetValues();

            for (var i = 0; i < fields.Length && i < values.Length; i++)
            {
                row[fields[i]] = values[i];
            }

            rows.Add(row);
        }

        var outputFields = new List<string>();
        var outputRows = new List<object?[]>();

        foreach (var row in rows)
        {
            var current = new List<object?>();
            foreach (var projection in _projections)
            {
                if (projection.Expression is StarIdentifierExpression star)
                {
                    foreach (var binding in star.Bindings.Count > 0 ? star.Bindings : row.Keys.Select(k => new ColumnBinding(new TableSymbol(string.Empty, Array.Empty<ColumnSymbol>()), new ColumnSymbol(k, string.Empty))))
                    {
                        var name = binding.ColumnSymbol.Name;
                        if (!outputFields.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            outputFields.Add(name);
                        }

                        current.Add(GetValue(row, binding.TableSymbol.TableName, binding.ColumnSymbol.Name));
                    }

                    continue;
                }

                var fieldName = projection.Alias ?? GetFieldName(projection.Expression);
                if (!outputFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                {
                    outputFields.Add(fieldName);
                }

                current.Add(NormalizeValue(EvaluateExpression(projection.Expression, row)));
            }

            outputRows.Add(current.ToArray());
        }

        return new EnumeratorExecutionReader(outputFields.ToArray(), outputRows.GetEnumerator(), Array.Empty<IAsyncDisposable>());
    }

    private static object? EvaluateExpression(Expression expression, IReadOnlyDictionary<string, object?> row)
    {
        switch (expression)
        {
            case LiteralExpression literal:
                return literal.Value;
            case IdentifierExpression identifier:
                if (identifier.Binding is not null)
                {
                    return GetValue(row, identifier.Binding.TableSymbol.TableName, identifier.Binding.ColumnSymbol.Name) ?? GetValue(row, null, identifier.Column);
                }

                return GetValue(row, null, identifier.Column);
            case BinaryOperatorExpression binary:
                var left = EvaluateExpression(binary.Left, row);
                var right = EvaluateExpression(binary.Right, row);
                return binary.Operator switch
                {
                    BinaryOperator.Add => Convert.ToDouble(left) + Convert.ToDouble(right),
                    BinaryOperator.Subtract => Convert.ToDouble(left) - Convert.ToDouble(right),
                    BinaryOperator.Multiply => Convert.ToDouble(left) * Convert.ToDouble(right),
                    BinaryOperator.Divide => Convert.ToDouble(left) / Convert.ToDouble(right),
                    BinaryOperator.Modulus => Convert.ToDouble(left) % Convert.ToDouble(right),
                    BinaryOperator.Concatenate => (left ?? string.Empty).ToString() + (right ?? string.Empty).ToString(),
                    _ => right,
                };
            case UnaryOperatorExpression unary:
                var value = EvaluateExpression(unary.Expression, row);
                return unary.Operator == UnaryOperator.Negate ? -Convert.ToDouble(value) : value;
            case FunctionCallExpression functionCall:
                if (functionCall.Arguments.Count == 1)
                {
                    var arg = EvaluateExpression(functionCall.Arguments[0], row);
                    return functionCall.FunctionName switch
                    {
                        "ABS" => Math.Abs(Convert.ToDouble(arg)),
                        _ => arg,
                    };
                }
                return null;
            default:
                return null;
        }
    }

    private static string GetFieldName(Expression expression)
        => expression switch
        {
            IdentifierExpression identifier => identifier.Column,
            FunctionCallExpression functionCall => functionCall.FunctionName,
            LiteralExpression => "Literal",
            _ => expression.GetType().Name,
        };

    private static object? GetValue(IReadOnlyDictionary<string, object?> row, string? tableName, string columnName)
    {
        if (tableName is not null)
        {
            var composite = $"{tableName}.{columnName}";
            if (row.TryGetValue(composite, out var compositeValue))
            {
                return NormalizeValue(compositeValue);
            }

            var suffixedMatches = row
                .Where(pair => pair.Key.EndsWith($".{columnName}", StringComparison.OrdinalIgnoreCase) && !pair.Key.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Value)
                .ToList();

            if (suffixedMatches.Count == 1)
            {
                return NormalizeValue(suffixedMatches[0]);
            }
        }

        if (row.TryGetValue(columnName, out var direct))
        {
            return NormalizeValue(direct);
        }

        foreach (var pair in row)
        {
            if (pair.Key.EndsWith($".{columnName}", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeValue(pair.Value);
            }
        }

        return null;
    }

    private static object? NormalizeValue(object? value)
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
}
