using System.Buffers;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

internal static class SubQueryExpansions
{
    public static async Task<object?> ExecuteScalarAsync(this ExecutionPlanNode node, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var reader = await node.ExecuteAsync(parameters, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var values = reader.GetValues();
            if (values.Length > 0)
            {
                return values[0];
            }
        }
        return null;
    }

    public static async Task<IExecutionReader> ExecuteWithSubqueriesAsync(this ExecutionPlanNode node, IEnumerable<ExpressionPlanNode> expressions, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        List<ExecutionPlanNode>? subQueries = null;

        foreach (var n in expressions.SelectMany(x => AllExpressions(x)).Distinct())
        {
            if (n is ScalarSubqueryExpressionExecutionPlan scalarSubqueryExpression)
            {
                subQueries ??= new List<ExecutionPlanNode>();

                subQueries.Add(scalarSubqueryExpression.Select);
            }
            // Implementation goes here
        }

        var inputReader = await node.ExecuteAsync(parameters, cancellationToken);
        if (subQueries is not null)
        {
            return new SubQueryExecutionReader(inputReader, subQueries);
        }

        return inputReader;
    }

    private static IEnumerable<ExpressionPlanNode> AllExpressions(ExpressionPlanNode node)
    {
        yield return node;

        foreach (var n in node.GetExpressions())
        {
            foreach (var e in AllExpressions(n))
            {
                yield return e;
            }
        }
    }
    private class SubQueryExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _inputReader;
        private readonly IReadOnlyList<ExecutionPlanNode> _subQueries;
        private List<ExecutionPlanNode>? _finalSubQueries = null;

        public SubQueryExecutionReader(IExecutionReader inputReader, IReadOnlyList<ExecutionPlanNode> subQueries)
        {
            _inputReader = inputReader;
            _subQueries = subQueries;
        }

        string?[]? _fields = null;
        int _fieldCount = 0;
        int _baseOffset = 0;
        object?[]? _currentRow = null;

        public override ReadOnlySpan<string?> GetFields()
            => _fields is null ? ReadOnlySpan<string?>.Empty : _fields.AsSpan(0, _fieldCount);

        public override ReadOnlySpan<object?> GetValues()
            => _currentRow is null ? ReadOnlySpan<object?>.Empty : _currentRow.AsSpan(0, _fieldCount);

        private Queue<object?[]>? _rows = null;

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (_fields is null)
            {
                var fields = _inputReader.GetFields();
                _baseOffset = fields.Length;
                _fieldCount = fields.Length + _subQueries.Count;
                _fields = ArrayPool<string?>.Shared.Rent(_fieldCount);
                fields.CopyTo(_fields.AsSpan(0, _baseOffset));
                _finalSubQueries = _subQueries.Where(x => !_fields.Contains(x.Identifier)).ToList();

                _fieldCount = fields.Length + _finalSubQueries.Count;
                // filter out where it already exists 
                for (var i = 0; i < _finalSubQueries.Count; i++)
                {
                    _fields[_baseOffset + i] = _finalSubQueries[i].Identifier;
                }
                _currentRow = ArrayPool<object?>.Shared.Rent(_fieldCount);
            }


            var read = await _inputReader.ReadAsync(cancellationToken);
            if (!read)
            {
                return false;
            }

            var vals = _inputReader.GetValues();
            vals.CopyTo(_currentRow.AsSpan(0, vals.Length));

            for (var i = 0; i < _finalSubQueries!.Count; i++)
            {
                var val = await _finalSubQueries[i].ExecuteScalarAsync(null, cancellationToken);
                _currentRow![_baseOffset + i] = val;
            }

            return true;
        }

        public override async ValueTask DisposeAsync()
        {
            await _inputReader.DisposeAsync();
            if (_currentRow is not null)
            {
                ArrayPool<object?>.Shared.Return(_currentRow);
            }
            if (_fields is not null)
            {
                ArrayPool<string?>.Shared.Return(_fields);
            }
        }
    }
}


