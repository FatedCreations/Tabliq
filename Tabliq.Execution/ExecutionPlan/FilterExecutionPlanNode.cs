using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class FilterExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly Condition? _condition;
    private readonly ConditionExecutionPlan _conditionPlan;

    public ExecutionPlanNode Input => _input;
    public Condition? Condition => _condition;
    public ConditionExecutionPlan ConditionPlan => _conditionPlan;

    public FilterExecutionPlanNode(ExecutionPlanNode input, ConditionExecutionPlan conditionPlan, Condition? condition = null)
    {
        _input = input;
        _conditionPlan = conditionPlan;
        _condition = condition;
    }

    public override IExecutionProvider? Provider => null;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. _input.GetExpressions(), .. ConditionPlan.GetExpressions()];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;
        if (newInput != _input)
        {
            currentNode = new FilterExecutionPlanNode(newInput, _conditionPlan, _condition);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var reader = await _input.ExecuteAsync(parameters, cancellationToken);

        async IAsyncEnumerable<object?[]> Filter()
        {
            var row = new object?[reader.GetFields().Length];

            while (await reader.ReadAsync(cancellationToken))
            {
                var fields = reader.GetFields();
                var values = reader.GetValues();

                values.CopyTo(row);

                if (_conditionPlan.Execute(new RowAccessor(fields, values)))
                {
                    yield return row;
                }
            }
        }

        var keys = reader.GetFields();
        return new AsyncEnumeratorExecutionReader(keys.ToArray(), Filter().GetAsyncEnumerator(cancellationToken), Array.Empty<IAsyncDisposable>());
    }
}
