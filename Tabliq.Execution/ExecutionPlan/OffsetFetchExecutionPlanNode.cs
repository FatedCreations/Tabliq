using System.Buffers;
using Tabliq.Execution.ExpressionPlan;

namespace Tabliq.Execution;

public sealed class OffsetFetchExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;

    public ExecutionPlanNode Input => _input;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() =>
        _input.GetExpressions()
        .Append(Fetch)
        .Append(Offset)
        .Where(x => x is not null)!;

    public OffsetFetchExecutionPlanNode(ExecutionPlanNode input, ExpressionPlanNode? fetch, ExpressionPlanNode? offset)
    {
        _input = input;
        Fetch = fetch;
        Offset = offset;
    }

    public ExpressionPlanNode? Fetch { get; }
    public ExpressionPlanNode? Offset { get; }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;

        if (newInput != _input)
        {
            currentNode = new OffsetFetchExecutionPlanNode(newInput, Fetch, Offset);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        // we need to determine all the column (outside of aggregates) referneced in the projection.
        return new OffsetFetchExecutionReader(await _input.ExecuteAsync(parameters, cancellationToken), Offset ?? new LiteralExpressionExecutionPlan(0), Fetch);
    }

    private class OffsetFetchExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _inputReader;
        private readonly ExpressionPlanNode? _offsetExpression;
        private readonly ExpressionPlanNode? _fetchExpression;
        private int _offset;
        private int? _fetch;

        public OffsetFetchExecutionReader(IExecutionReader inputReader, ExpressionPlanNode? offset, ExpressionPlanNode? fetch)
        {
            _inputReader = inputReader;
            _offsetExpression = offset;
            _fetchExpression = fetch;
        }

        public override ReadOnlySpan<string> GetFields() => _inputReader.GetFields();

        public override ReadOnlySpan<object?> GetValues()
            => _inputReader.GetValues();

        private int _position = -1;
        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (_position == -1)
            {
                _offset = _offsetExpression is null ? 0 : Convert.ToInt32(_offsetExpression.ExecuteExpression(RowAccessor.Empty, null));
                _fetch = _fetchExpression is null ? null : Convert.ToInt32(_fetchExpression.ExecuteExpression(RowAccessor.Empty, null));

                // consume to first offset position
                _position++;
                while (_position < _offset)
                {
                    if (!await _inputReader.ReadAsync(cancellationToken))
                    {
                        return false;
                    }
                    _position++;
                }
            }

            if (_fetch is not null && _position >= _offset + _fetch.Value)
            {
                return false;
            }

            return await _inputReader.ReadAsync(cancellationToken);
        }
    }

}
