using System.Buffers;
using System.Security.Cryptography.X509Certificates;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public sealed class ProjectionColumnPlan
{
    public required ExpressionPlanNode Value { get; init; }

    public required string Alias { get; init; }

    public TableSymbol? TableSymbol => Value is IdentifierExpressionExecutionPlan identifierPlan
        ? identifierPlan.TableSymbol
        : null;

    public ColumnSymbol? ColumnSymbol => Value is IdentifierExpressionExecutionPlan identifierPlan
        ? identifierPlan.ColumnSymbol
        : null;
}

public sealed class ProjectionExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly IReadOnlyList<ProjectionColumnPlan> _projections;

    public IReadOnlyList<ProjectionColumnPlan> Projections => _projections;

    public ExecutionPlanNode Input => _input;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() =>
        _input.GetExpressions()
            .Concat(_projections.Select(p => p.Value));

    public ProjectionExecutionPlanNode(ExecutionPlanNode input, IReadOnlyList<ProjectionColumnPlan> projections)
    {
        _input = input;
        _projections = projections;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;

        List<ProjectionColumnPlan>? newProjections = new List<ProjectionColumnPlan>();
        var hasNewProjections = false;
        foreach (var projection in _projections)
        {
            var val = projection.Value.TryRewrite(context) ?? projection.Value;

            if (val != projection.Value)
            {
                hasNewProjections = true;
                newProjections.Add(new ProjectionColumnPlan { Value = val, Alias = projection.Alias });
            }
            else
            {
                newProjections.Add(projection);
            }
        }
        if (newInput != _input || hasNewProjections)
        {
            currentNode = new ProjectionExecutionPlanNode(newInput, newProjections);
        }

        // todo apply rewriting rule for the expressions etc too

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        return new ProjectionExecutionReader(await _input.ExecuteAsync(parameters, cancellationToken), Projections);
    }


    private class ProjectionExecutionReader : BaseExecutionReader
    {
        private readonly IExecutionReader _inputReader;
        private readonly IReadOnlyList<ProjectionColumnPlan> _projections;
        private readonly string[] _outputFields;
        private readonly object?[] _buffer;

        private int _fieldCount;

        public ProjectionExecutionReader(IExecutionReader inputReader, IReadOnlyList<ProjectionColumnPlan> projections)
        {
            _inputReader = inputReader;

            _fieldCount = projections.Count;

            _projections = projections;

            _outputFields = ArrayPool<string>.Shared.Rent(_fieldCount);
            for (var i = 0; i < _fieldCount; i++)
            {
                var projection = _projections[i];
                var fieldName = projection.Alias ?? throw new Exception("name should already be defined!");// GetFieldName(projection.Value);
                _outputFields[i] = fieldName;
            }

            _buffer = ArrayPool<object?>.Shared.Rent(_fieldCount);
        }


        public override ReadOnlySpan<string> GetFields() => _outputFields.AsSpan(0, _fieldCount);

        public override ReadOnlySpan<object?> GetValues()
        {
            var accessor = _inputReader.GetRowAccessor();
            for (var i = 0; i < _fieldCount; i++)
            {
                var projection = _projections[i];
                _buffer[i] = projection.Value.ExecuteExpression(accessor, null);
            }
            return _buffer.AsSpan(0, _fieldCount);
        }

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return await _inputReader.ReadAsync(cancellationToken);
        }
        public override async ValueTask DisposeAsync()
        {
            await _inputReader.DisposeAsync();
            ArrayPool<object?>.Shared.Return(_buffer);
            ArrayPool<string>.Shared.Return(_outputFields);
        }
    }
}
