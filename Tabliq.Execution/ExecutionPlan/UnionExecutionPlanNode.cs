using System.Globalization;
using System.Text;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;

namespace Tabliq.Execution;

public sealed class UnionExecutionPlanNode : ExecutionPlanNode
{
    private readonly IReadOnlyList<(ExecutionPlanNode Input, bool IsAll)> _operations;

    public IReadOnlyList<(ExecutionPlanNode Input, bool IsAll)> Operations => _operations;

    public UnionExecutionPlanNode(IEnumerable<(ExecutionPlanNode Input, bool IsAll)> operations)
    {
        _operations = operations.ToList();
    }

    public override IExecutionProvider? Provider => null;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => _operations.Select(x => x.Input);
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => GetInputs().SelectMany(x => x.GetExpressions());

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        var rewritten = new List<(ExecutionPlanNode Input, bool IsAll)>();
        var changed = false;

        foreach (var operation in _operations)
        {
            var rewrittenInput = operation.Input.TryRewrite(context) ?? operation.Input;
            if (rewrittenInput != operation.Input)
            {
                changed = true;
            }

            rewritten.Add((rewrittenInput, operation.IsAll));
        }

        if (rewritten.Count > 0)
        {
            var unionWithRewrittenBranches = new UnionExecutionPlanNode(rewritten);
            foreach (var operation in rewritten)
            {
                if (operation.Input.Provider is not null)
                {
                    var providerRewritten = operation.Input.Provider.TryRewrite(unionWithRewrittenBranches, context);
                    if (providerRewritten is not null && providerRewritten != unionWithRewrittenBranches)
                    {
                        return providerRewritten;
                    }
                }
            }
        }

        if (!changed)
        {
            return null;
        }

        return new UnionExecutionPlanNode(rewritten);
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (_operations.Count == 0)
        {
            return new EnumeratorExecutionReader(Array.Empty<string>(), ((IEnumerable<object?[]?>)Array.Empty<object?[]>()).GetEnumerator(), Array.Empty<IAsyncDisposable>());
        }

        var fieldNames = Array.Empty<string>();
        var rows = new List<object?[]?>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (input, isAll) in _operations)
        {
            await using var reader = await input.ExecuteAsync(parameters, cancellationToken);
            if (fieldNames.Length == 0)
            {
                fieldNames = reader.GetFields().ToArray();
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = reader.GetValues().ToArray();
                var rowKey = BuildRowKey(row);

                if (!isAll && !seen.Add(rowKey))
                {
                    continue;
                }

                rows.Add(row);
                seen.Add(rowKey);
            }
        }

        return new EnumeratorExecutionReader(fieldNames, rows.GetEnumerator(), Array.Empty<IAsyncDisposable>());
    }

    private static string BuildRowKey(object?[] row)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < row.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('\u001F');
            }

            var value = row[i];
            if (value is null)
            {
                builder.Append("<null>");
                continue;
            }

            if (value is string s)
            {
                builder.Append(s);
                continue;
            }

            if (value is bool b)
            {
                builder.Append(b ? "1" : "0");
                continue;
            }

            if (value is IFormattable formattable)
            {
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                continue;
            }

            builder.Append(value.GetType().FullName);
            builder.Append(':');
            builder.Append(value);
        }

        return builder.ToString();
    }
}
