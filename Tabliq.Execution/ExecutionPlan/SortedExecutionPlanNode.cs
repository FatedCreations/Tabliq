using System.Buffers;
using Tabliq.Execution.ExpressionPlan;

namespace Tabliq.Execution;

public sealed record SortedPlanColumn
{
    public required ExpressionPlanNode Expression { get; init; }
    public required bool Ascending { get; init; }
}

public sealed class SortedExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly IReadOnlyList<SortedPlanColumn> _columns;

    public IReadOnlyList<SortedPlanColumn> Columns => _columns;

    public ExecutionPlanNode Input => _input;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() =>
        _input.GetExpressions()
            .Concat(_columns.Select(c => c.Expression));

    public SortedExecutionPlanNode(ExecutionPlanNode input, IReadOnlyList<SortedPlanColumn> columns)
    {
        _input = input;
        _columns = columns;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;

        List<SortedPlanColumn> newProjections = new List<SortedPlanColumn>();
        var hasNewProjections = false;
        foreach (var projection in _columns)
        {
            var val = projection.Expression.TryRewrite(context) ?? projection.Expression;

            if (val != projection.Expression)
            {
                hasNewProjections = true;
                newProjections.Add(new SortedPlanColumn { Expression = val, Ascending = projection.Ascending });
            }
            else
            {
                newProjections.Add(projection);
            }
        }
        if (newInput != _input || hasNewProjections)
        {
            currentNode = new SortedExecutionPlanNode(newInput, newProjections);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        // we need to determine all the column (outside of aggregates) referneced in the projection.
        return new SortedExecutionReader(await _input.ExecuteWithSubqueriesAsync(_columns.Select(x=>x.Expression), parameters, cancellationToken), _columns);
    }

    private class SortedExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _inputReader;
        private readonly IReadOnlyList<SortedPlanColumn> _columns;

        public SortedExecutionReader(IExecutionReader inputReader, IReadOnlyList<SortedPlanColumn> columns)
        {
            _inputReader = inputReader;
            _columns = columns;
        }

        public override ReadOnlySpan<string> GetFields() => _inputReader.GetFields();

        public override ReadOnlySpan<object?> GetValues()
            => _currentRow.AsSpan(0, _fieldCount);

        object?[]? _currentRow = null;
        int _fieldCount = 0;
        private Queue<object?[]>? _rows = null;

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (_rows is null)
            {
                await ComputeData(cancellationToken);
            }

            if (_currentRow is not null)
            {
                ArrayPool<object?>.Shared.Return(_currentRow);
                _currentRow = null;
            }

            return _rows!.TryDequeue(out _currentRow);
        }

        private class SortComparer : IComparer<object?[]>
        {
            private readonly IReadOnlyList<SortedPlanColumn> _columns;

            public SortComparer(IReadOnlyList<SortedPlanColumn> columns)
            {
                _columns = columns;
            }

            public int Compare(object?[]? x, object?[]? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                for (int i = 0; i < _columns.Count; i++)
                {
                    var column = _columns[i];
                    var comparison = Comparer<object?>.Default.Compare(x[i], y[i]);
                    if (comparison != 0)
                    {
                        return column.Ascending ? comparison : -comparison;
                    }
                }
                return 0;
            }
        }

        private async Task ComputeData(CancellationToken token)
        {
            _fieldCount = _inputReader.GetFields().Length;

            var comp = new SortComparer(_columns);
            var sortedList = new SortedList<object?[], object?[]>(comp);


            while (await _inputReader.ReadAsync(token))
            {
                var row = _inputReader.GetRowAccessor();
                var rowVals = _inputReader.GetValues();

                var tempRowBuffer = ArrayPool<object?>.Shared.Rent(_fieldCount);
                var tempSortBuffer = ArrayPool<object?>.Shared.Rent(_columns.Count);

                rowVals.CopyTo(tempRowBuffer);

                for (var i = 0; i < _columns.Count; i++)
                {
                    var projection = _columns[i];
                    tempSortBuffer[i] = ExpressionPlan.ExpressionPlanNode.NormalizeValue(projection.Expression.ExecuteExpression(row, null));
                }

                sortedList.Add(tempSortBuffer, tempRowBuffer);
            }

            _rows = new Queue<object?[]>(sortedList.Values);

            foreach (var r in sortedList.Keys)
            {
                ArrayPool<object?>.Shared.Return(r);
            }
            sortedList = null;

        }
        public override async ValueTask DisposeAsync()
        {
            await _inputReader.DisposeAsync();
            if (_currentRow is not null)
            {
                ArrayPool<object?>.Shared.Return(_currentRow);
            }
        }
    }

}
