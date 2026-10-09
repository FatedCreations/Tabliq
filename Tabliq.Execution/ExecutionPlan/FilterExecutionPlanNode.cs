using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class FilterExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly ConditionExecutionPlan _conditionPlan;

    public ExecutionPlanNode Input => _input;
    public ConditionExecutionPlan ConditionPlan => _conditionPlan;

    public FilterExecutionPlanNode(ExecutionPlanNode input, ConditionExecutionPlan conditionPlan)
    {
        _input = input;
        _conditionPlan = conditionPlan;
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
            currentNode = new FilterExecutionPlanNode(newInput, _conditionPlan);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => new FilteredExecutionReader(await _input.ExecuteAsync(parameters, cancellationToken), _conditionPlan);   

    private class FilteredExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _reader;
        private readonly ConditionExecutionPlan _conditionPlan;
        public FilteredExecutionReader(IExecutionReader reader, ConditionExecutionPlan conditionPlan)
        {
            _reader = reader;
            _conditionPlan = conditionPlan;
        }
        public override ReadOnlySpan<string> GetFields() => _reader.GetFields();
        public override  ReadOnlySpan<object?> GetValues() => _reader.GetValues();
        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            while (await _reader.ReadAsync(cancellationToken))
            {
                if (_conditionPlan.Execute(GetRowAccessor()))
                {
                    return true;
                }
            }
            return false;
        }
        public override async ValueTask DisposeAsync()
        {
            await _reader.DisposeAsync();
        }
    }
}
