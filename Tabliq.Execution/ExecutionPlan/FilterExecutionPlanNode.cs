using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class FilterExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly Condition _condition;

    public ExecutionPlanNode Input => _input;
    public Condition Condition => _condition;

    public FilterExecutionPlanNode(ExecutionPlanNode input, Condition condition)
    {
        _input = input;
        _condition = condition;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;
        if (newInput != _input)
        {
            currentNode = new FilterExecutionPlanNode(newInput, _condition);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var reader = await _input.ExecuteAsync(parameters, cancellationToken);

        async IAsyncEnumerable<object?[]> Filter()
        {
            if (_input is EmptyExecutionPlanNode)
            {
                if (EvaluationHelpers.EvaluateCondition(_condition, new RowAccessor(Array.Empty<string>(), Array.Empty<object?>())))
                {
                    yield return Array.Empty<object?>();
                }

                yield break;
            }

            var row = new object?[reader.GetFields().Length];

            while (await reader.ReadAsync(cancellationToken))
            {
                var fields = reader.GetFields();
                var values = reader.GetValues();

                values.CopyTo(row);

                if (EvaluationHelpers.EvaluateCondition(_condition, new RowAccessor(fields, values)))
                {
                    yield return row;
                }
            }
        }

        var keys = reader.GetFields();
        return new AsyncEnumeratorExecutionReader(keys.ToArray(), Filter().GetAsyncEnumerator(cancellationToken), Array.Empty<IAsyncDisposable>());
    }
}
