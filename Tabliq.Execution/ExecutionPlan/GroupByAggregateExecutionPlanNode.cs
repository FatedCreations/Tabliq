using System.Buffers;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Execution.Functions;

namespace Tabliq.Execution;

public sealed class GroupByAggregateExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly IReadOnlyList<ExpressionPlanNode> _groups;

    public IReadOnlyList<ExpressionPlanNode> Groups => _groups;

    private readonly IReadOnlyList<AggregateFunctionCallExpressionExecutionPlan> _aggregates;

    public IReadOnlyList<AggregateFunctionCallExpressionExecutionPlan> Aggregates => _aggregates;

    public ExecutionPlanNode Input => _input;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() =>
        _input.GetExpressions()
            .Concat(_groups)
            .Concat(_aggregates);

    public GroupByAggregateExecutionPlanNode(ExecutionPlanNode input, IReadOnlyList<ExpressionPlanNode> groups, IReadOnlyList<AggregateFunctionCallExpressionExecutionPlan> aggregates)
    {
        _input = input;
        _groups = groups;
        _aggregates = aggregates;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;

        List<ExpressionPlanNode>? newGroups = new List<ExpressionPlanNode>();
        var hasNewGroup = false;
        foreach (var group in _groups)
        {
            var val = group.TryRewrite(context) ?? group;

            if (val != group)
            {
                hasNewGroup = true;
            }

            newGroups.Add(val);
        }

        List<AggregateFunctionCallExpressionExecutionPlan>? newAggregates = new List<AggregateFunctionCallExpressionExecutionPlan>();
        var hasNewAggregate = false;
        foreach (var agg in _aggregates)
        {
            var val = agg.TryRewrite(context) as AggregateFunctionCallExpressionExecutionPlan ?? agg;

            if (val != agg)
            {
                hasNewAggregate = true;
            }

            newAggregates.Add(val);
        }

        if (newInput != _input || hasNewGroup || hasNewAggregate)
        {
            currentNode = new GroupByAggregateExecutionPlanNode(newInput, newGroups, newAggregates);
        }

        // todo apply rewriting rule for the expressions etc too

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        // we need to determine all the column (outside of aggregates) referneced in the projection.


        return new GroupByAggregateExecutionReader(await _input.ExecuteAsync(parameters, cancellationToken), _groups, _aggregates);
    }


    private class GroupByAggregateExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _inputReader;
        private readonly IReadOnlyList<ExpressionPlanNode> _groups;
        private readonly IReadOnlyList<AggregateFunctionCallExpressionExecutionPlan> _aggregates;
        private readonly string[] _outputFields;
        private int _fieldCount;

        public GroupByAggregateExecutionReader(IExecutionReader inputReader, IReadOnlyList<ExpressionPlanNode> groups, IReadOnlyList<AggregateFunctionCallExpressionExecutionPlan> aggregates)
        {
            _inputReader = inputReader;
            _groups = groups;
            _aggregates = aggregates;

            _fieldCount = _groups.Count + _aggregates.Count;

            _outputFields = ArrayPool<string>.Shared.Rent(_fieldCount);
            for (var i = 0; i < _groups.Count; i++)
            {
                var projection = _groups[i];
                _outputFields[i] = projection.Identifier;
            }

            for (var i = 0; i < _aggregates.Count; i++)
            {
                var projection = _aggregates[i];
                _outputFields[_groups.Count + i] = projection.Identifier;
            }
        }


        public override ReadOnlySpan<string> GetFields() => _outputFields.AsSpan(0, _fieldCount);

        public override ReadOnlySpan<object?> GetValues()
            => _currentRow.AsSpan(0, _fieldCount);

        object?[]? _currentRow = null;
        private Queue<object?[]>? _rows = null;

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (_rows is null)
            {
                await ComputeGroups(cancellationToken);
            }

            if (_currentRow is not null)
            {
                ArrayPool<object?>.Shared.Return(_currentRow);
                _currentRow = null;
            }

            return _rows!.TryDequeue(out _currentRow);
        }

        private async Task ComputeGroups(CancellationToken token)
        {
            _rows = new Queue<object?[]>();

            var tempBuffer = ArrayPool<object?>.Shared.Rent(_fieldCount);
            Dictionary<int, (object?[] Values, Dictionary<string, AggregateFunctionState> States)> aggregateStates = new();

            while (await _inputReader.ReadAsync(token))
            {
                var row = _inputReader.GetRowAccessor();
                // we need to evaluate the group expressions and the aggregate expressions for the current row.
                var code = new HashCode();
                for (var i = 0; i < _groups.Count; i++)
                {
                    var projection = _groups[i];
                    tempBuffer[i] = ExpressionPlan.ExpressionPlanNode.NormalizeValue(projection.ExecuteExpression(row, null));
                    code.Add(tempBuffer[i]);
                }

                var groupKey = code.ToHashCode();
                if (!aggregateStates.TryGetValue(groupKey, out var aggregateState))
                {
                    aggregateState = (tempBuffer, new Dictionary<string, AggregateFunctionState>());
                    _rows.Enqueue(tempBuffer);
                    tempBuffer = ArrayPool<object?>.Shared.Rent(_fieldCount);
                    aggregateStates[groupKey] = aggregateState;
                }

                for (var i = 0; i < _aggregates.Count; i++)
                {
                    var projection = _aggregates[i];

                    if (!aggregateState.States.TryGetValue(projection.Identifier, out var state))
                    {
                        state = projection.InitState();
                        aggregateState.States[projection.Identifier] = state;
                    }
                    aggregateState.States[projection.Identifier] = projection.ProcessRow(row, state);
                }
            }

            foreach (var (row, states) in aggregateStates.Values)
            {
                //finalise the aggregates!
                for (var i = 0; i < _aggregates.Count; i++)
                {
                    var projection = _aggregates[i];

                    if (!states.TryGetValue(projection.Identifier, out var state))
                    {
                        throw new Exception($"State for aggregate '{projection.Identifier}' not found.");
                    }
                    row[_groups.Count + i] = state.GetAggregateValue();
                }
            }

            aggregateStates.Clear();
        }
        public override async ValueTask DisposeAsync()
        {
            await _inputReader.DisposeAsync();
            if (_currentRow is not null)
            {
                ArrayPool<object?>.Shared.Return(_currentRow);
            }

            ArrayPool<string>.Shared.Return(_outputFields);
        }
    }

}
