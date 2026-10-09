using System.Globalization;
using System.Text;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class UnionAllExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input1;
    private readonly ExecutionPlanNode _input2;
    private readonly bool _isAll;
    public ExecutionPlanNode Input1 => _input1;
    public ExecutionPlanNode Input2 => _input2;

    public UnionAllExecutionPlanNode(ExecutionPlanNode Input1, ExecutionPlanNode Input2)
    {
        _input1 = Input1;
        _input2 = Input2;
    }

    public override IExecutionProvider? Provider => null;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input1, _input2];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => GetInputs().SelectMany(x => x.GetExpressions());

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newLeft = _input1.TryRewrite(context) ?? _input1;
        var newRight = _input2.TryRewrite(context) ?? _input2;
        if (newLeft != _input1 || newRight != _input2)
        {
            currentNode = new UnionAllExecutionPlanNode(newLeft, newRight);
        }

        var provider = newLeft.Provider ?? newRight.Provider;
        currentNode = provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        return new UnionExecutionReader(await _input1.ExecuteAsync(parameters, cancellationToken), await _input2.ExecuteAsync(parameters, cancellationToken));
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

    private class UnionExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _reader1;
        private readonly IExecutionReader _reader2;
        private bool _reader1Exhausted = false;

        public UnionExecutionReader(IExecutionReader reader1, IExecutionReader reader2)
        {
            _reader1 = reader1;
            _reader2 = reader2;
        }

        public override async ValueTask DisposeAsync()
        {
            await _reader1.DisposeAsync();
            await _reader2.DisposeAsync();
        }

        bool validated = false;
        public override ReadOnlySpan<string> GetFields()
        {
            if (!validated)
            {
                var fields1 = _reader1.GetFields();
                var fields2 = _reader2.GetFields();
                if (!fields1.SequenceEqual(fields2))
                {
                    throw new InvalidOperationException("Field names do not match.");
                }
                validated = true;

                return fields1;
            }

            return _reader1.GetFields();
        }

        public override ReadOnlySpan<object?> GetValues()
        {
            if (!_reader1Exhausted)
            {
                return _reader1.GetValues();
            }
            return _reader2.GetValues();
        }

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (!_reader1Exhausted)
            {
                var hasMore = await _reader1.ReadAsync(cancellationToken);
                if (hasMore)
                {
                    return true;
                }
                _reader1Exhausted = true;
            }

            return await _reader2.ReadAsync(cancellationToken);
        }
    }
}

public sealed class DistinctExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    public ExecutionPlanNode Input => _input;

    public DistinctExecutionPlanNode(ExecutionPlanNode Input)
    {
        _input = Input;
    }

    public override IExecutionProvider? Provider => null;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => GetInputs().SelectMany(x => x.GetExpressions());

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;
        if (newInput != _input)
        {
            currentNode = new DistinctExecutionPlanNode(newInput);
        }

        var provider = newInput.Provider;
        currentNode = provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        return new DistinctExecutionReader(await _input.ExecuteAsync(parameters, cancellationToken));
    }

    private class DistinctExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _reader;

        private List<int> _seenRowHashes = new List<int>();

        public DistinctExecutionReader(IExecutionReader reader)
        {
            _reader = reader;
        }

        public override async ValueTask DisposeAsync()
        {
            await _reader.DisposeAsync();
        }

        public override ReadOnlySpan<string> GetFields()
        {
            return _reader.GetFields();
        }

        public override ReadOnlySpan<object?> GetValues()
        {
            return _reader.GetValues();
        }

        public int HashRow(ReadOnlySpan<object?> row)
        {
            var c = new HashCode();
            for (var i = 0; i < row.Length; i++)
            {
                c.Add(row[i]);
            }
            return c.ToHashCode();
        }

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            // get next row from reader, and check if we have seen it before, if so, keep reading until we find a new row or exhaust the reader
            while (true)
            {
                var hasMore = await _reader.ReadAsync(cancellationToken);
                if (!hasMore)
                {
                    return false;
                }

                var rowHash = HashRow(_reader.GetValues());
                if (!_seenRowHashes.Contains(rowHash))
                {
                    _seenRowHashes.Add(rowHash);
                    return true;
                }
            } 
        }
    }
}


