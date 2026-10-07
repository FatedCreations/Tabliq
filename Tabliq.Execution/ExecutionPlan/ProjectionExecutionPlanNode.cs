using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public sealed class ProjectionExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _input;
    private readonly IReadOnlyList<SelectProjection> _projections;
    private readonly IReadOnlyList<ExpressionPlan.ExpressionPlanNode> _expressionPlans;

    public SelectExpression? SourceSelect { get; }

    public IReadOnlyList<SelectProjection> Projections => _projections;
    public IReadOnlyList<ExpressionPlan.ExpressionPlanNode> ExpressionPlans => _expressionPlans;

    public ExecutionPlanNode Input => _input;
    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_input];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. _input.GetExpressions(), .. _expressionPlans];

    public ProjectionExecutionPlanNode(ExecutionPlanNode input, IReadOnlyList<ExpressionPlan.ExpressionPlanNode> expressionPlans, IReadOnlyList<SelectProjection>? projections, SelectExpression? sourceSelect = null)
    {
        _input = input;
        _expressionPlans = expressionPlans;
        _projections = projections ?? Array.Empty<SelectProjection>();
        SourceSelect = sourceSelect;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInput = _input.TryRewrite(context) ?? _input;
        if (newInput != _input)
        {
            currentNode = new ProjectionExecutionPlanNode(newInput, _expressionPlans, _projections, SourceSelect);
        }

        currentNode = newInput.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        var hasGroupBy = SourceSelect?.GroupBy is not null;

        if (_input is EmptyExecutionPlanNode || _input is FilterExecutionPlanNode { Input: EmptyExecutionPlanNode })
        {
            var emptyRow = new RowAccessor(Array.Empty<string>(), Array.Empty<object?>());
            if (_input is FilterExecutionPlanNode filter && filter.Input is EmptyExecutionPlanNode)
            {
                if (!filter.ConditionPlan.Execute(emptyRow))
                {
                    return new EnumeratorExecutionReader(GetProjectedFields(Array.Empty<string>()), new List<object?[]?>().GetEnumerator(), Array.Empty<IAsyncDisposable>());
                }
            }

            var singleRowOutputFields = GetProjectedFields(Array.Empty<string>());

            if (ContainsAggregateProjection())
            {
                var aggregateCalls = GetAggregateFunctionCalls();
                var aggregateStates = new Dictionary<FunctionCallExpression, AggregateFunctionState>();
                foreach (var aggregateCall in aggregateCalls)
                {
                    aggregateStates[aggregateCall.Expression] = aggregateCall.Function.InitState();
                }

                var aggregateValues = aggregateStates.ToDictionary(x => x.Key, x => x.Value.GetAggregateValue(cancellationToken));
                var aggregateRow = ProjectRow(emptyRow, aggregateValues);
                return new EnumeratorExecutionReader(singleRowOutputFields, new List<object?[]?> { aggregateRow }.GetEnumerator(), Array.Empty<IAsyncDisposable>());
            }

            var constantRow = ProjectRow(emptyRow, null);
            return new EnumeratorExecutionReader(singleRowOutputFields, new List<object?[]?> { constantRow }.GetEnumerator(), Array.Empty<IAsyncDisposable>());
        }

        if (ContainsAggregateProjection())
        {
            await using var aggregateReader = await _input.ExecuteAsync(parameters, cancellationToken);
            var aggregateOutputFields = GetProjectedFields(aggregateReader.GetFields());

            if (hasGroupBy)
            {
                var groupedRows = await ExecuteGroupedProjection(aggregateReader, cancellationToken);
                return new EnumeratorExecutionReader(aggregateOutputFields, groupedRows.GetEnumerator(), Array.Empty<IAsyncDisposable>());
            }

            var outputRow = await ExecuteAggregateProjection(aggregateReader, aggregateOutputFields, cancellationToken);
            return new EnumeratorExecutionReader(aggregateOutputFields, Enumerable.Repeat(outputRow, 1).GetEnumerator(), Array.Empty<IAsyncDisposable>());
        }

        var streamReader = await _input.ExecuteAsync(parameters, cancellationToken);
        var outputFields = GetProjectedFields(streamReader.GetFields());

        async IAsyncEnumerable<object?[]> ReadRowsAsync()
        {
            while (await streamReader.ReadAsync(cancellationToken))
            {
                var accessor = streamReader.GetRowAccessor();
                yield return ProjectRow(accessor, null);
            }
        }

        return new AsyncEnumeratorExecutionReader(outputFields, ReadRowsAsync().GetAsyncEnumerator(cancellationToken), [streamReader]);
    }

    private async Task<List<object?[]>> ExecuteGroupedProjection(IExecutionReader executionReader, CancellationToken cancellationToken)
    {
        var groupedRows = new List<GroupProjectionState>();
        var groups = new Dictionary<string, GroupProjectionState>(StringComparer.OrdinalIgnoreCase);
        var aggregateCalls = GetAggregateFunctionCalls();

        var groupByEntries = SourceSelect?.GroupBy?.Entries ?? [];

        while (await executionReader.ReadAsync(cancellationToken))
        {
            var accessor = executionReader.GetRowAccessor();
            var groupKey = BuildGroupKey(accessor, groupByEntries);
            if (!groups.TryGetValue(groupKey, out var state))
            {
                state = new GroupProjectionState(accessor);
                groups[groupKey] = state;
                groupedRows.Add(state);
            }

            foreach (var aggregateCall in aggregateCalls)
            {

                if (!state.AggregateStates.TryGetValue(aggregateCall.Expression, out var priorState))
                {
                    priorState = aggregateCall.Function.InitState();
                    state.AggregateStates[aggregateCall.Expression] = priorState;
                }
                state.AggregateStates[aggregateCall.Expression] = aggregateCall.Function.ProcessRow(aggregateCall.Expression, accessor, aggregateCall.Arguments, priorState);
            }
        }

        var result = new List<object?[]>();
        foreach (var group in groupedRows)
        {
            var aggregateValues = group.AggregateStates.ToDictionary(x => x.Key, x => x.Value?.GetAggregateValue(cancellationToken));
            result.Add(ProjectRow(group.RepresentativeRow, aggregateValues));
        }

        return result;
    }

    private static string BuildGroupKey(RowAccessor row, IReadOnlyList<Expression> groupByEntries)
    {
        if (groupByEntries.Count == 0)
        {
            return "__all__";
        }

        var parts = new List<string>();
        foreach (var groupEntry in groupByEntries)
        {
            var value = ExpressionPlan.ExpressionPlanNode.Create(groupEntry).Execute(row);
            parts.Add(value is null ? "<null>" : $"{value.GetType().FullName}:{value}");
        }

        return string.Join("|", parts);
    }

    private async Task<object?[]> ExecuteAggregateProjection(IExecutionReader executionReader, string[] outputFields, CancellationToken cancellationToken)
    {
        var aggregateCalls = GetAggregateFunctionCalls();
        var aggregateStates = new Dictionary<FunctionCallExpression, AggregateFunctionState>();

        object?[]? firstRow = null;
        while (await executionReader.ReadAsync(cancellationToken))
        {
            var accessor = executionReader.GetRowAccessor();

            if (firstRow is null)
            {
                firstRow = new object?[executionReader.GetFields().Length];
                executionReader.GetValues().CopyTo(firstRow);
            }

            foreach (var aggregateCall in aggregateCalls)
            {
                if (!aggregateStates.TryGetValue(aggregateCall.Expression, out var state))
                {
                    state = aggregateCall.Function.InitState();
                    aggregateStates[aggregateCall.Expression] = state;
                }
                aggregateStates[aggregateCall.Expression] = aggregateCall.Function.ProcessRow(aggregateCall.Expression, accessor, aggregateCall.Arguments, state);
            }
        }

        var aggregateValues = aggregateStates.ToDictionary(x => x.Key, x => x.Value?.GetAggregateValue(cancellationToken));
        var rowAccessor = firstRow is null
            ? new RowAccessor(executionReader.GetFields(), Array.Empty<object?>())
            : new RowAccessor(executionReader.GetFields(), firstRow);

        return ProjectRow(rowAccessor, aggregateValues);
    }

    private sealed class GroupProjectionState
    {
        private readonly string[] _columns;
        private readonly object?[] _row;

        public GroupProjectionState(RowAccessor accessor)
        {
            _columns = accessor.Columns.ToArray();
            _row = new object?[accessor.Columns.Length];
            for (var i = 0; i < accessor.Columns.Length; i++)
            {
                _row[i] = accessor[accessor.Columns[i]];
            }

            AggregateStates = new Dictionary<FunctionCallExpression, AggregateFunctionState>();
        }

        public IDictionary<FunctionCallExpression, AggregateFunctionState> AggregateStates { get; }

        public RowAccessor RepresentativeRow => new RowAccessor(_columns, _row);
    }

    private object?[] ProjectRow(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues)
    {
        var current = new List<object?>();
        for (var i = 0; i < _projections.Count; i++)
        {
            var projection = _projections[i];
            if (projection.Expression is StarIdentifierExpression star)
            {
                var bindings = star.Bindings.Count == 0
                    ? ExpandStarBindings(row.Columns)
                    : star.Bindings;

                foreach (var binding in bindings)
                {
                    if (binding.ColumnSymbol.ExcludeFromStarExpansion)
                    {
                        continue;
                    }

                    current.Add(GetValue(row, binding.TableSymbol.TableName, binding.ColumnSymbol.Name));
                }

                continue;
            }

            var plan = _expressionPlans.Count > i ? _expressionPlans[i] : ExpressionPlan.ExpressionPlanNode.Create(projection.Expression);
            current.Add(ExpressionPlan.ExpressionPlanNode.NormalizeValue(plan.Execute(row, aggregateValues)));
        }

        return current.ToArray();
    }

    private string[] GetProjectedFields(ReadOnlySpan<string> inputFields)
    {
        var outputFields = new List<string>();
        foreach (var projection in _projections)
        {
            if (projection.Expression is StarIdentifierExpression star)
            {
                var bindings = star.Bindings.Count == 0
                    ? ExpandStarBindings(inputFields)
                    : star.Bindings;

                foreach (var binding in bindings)
                {
                    var name = binding.ColumnSymbol.Name;
                    if (!outputFields.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        outputFields.Add(name);
                    }
                }

                continue;
            }

            var fieldName = projection.Alias ?? GetFieldName(projection.Expression);
            if (!outputFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                outputFields.Add(fieldName);
            }
        }

        return outputFields.ToArray();
    }

    private static IReadOnlyList<ColumnBinding> ExpandStarBindings(ReadOnlySpan<string> inputFields)
    {
        var bindings = new List<ColumnBinding>();
        foreach (var column in inputFields)
        {
            bindings.Add(new ColumnBinding(new TableSymbol(string.Empty, Array.Empty<ColumnSymbol>()), new ColumnSymbol(column, string.Empty)));
        }

        return bindings;
    }

    private bool ContainsAggregateProjection()
        => _projections.Any(x => ContainsAggregateFunction(x.Expression));

    private List<AggregateFunctionCallExpressionExecutionPlan> GetAggregateFunctionCalls()
    {
        var aggregateFunctions = new List<AggregateFunctionCallExpressionExecutionPlan>();
        foreach (var projection in _expressionPlans)
        {
            CollectAggregateFunctions(projection, aggregateFunctions);
        }

        return aggregateFunctions;
    }

    private static void CollectAggregateFunctions(ExpressionPlan.ExpressionPlanNode expressionPlan, List<AggregateFunctionCallExpressionExecutionPlan> aggregateFunctions)
    {
        if (expressionPlan is AggregateFunctionCallExpressionExecutionPlan aggregateFunction)
        {
            aggregateFunctions.Add(aggregateFunction);
        }

        foreach (var child in expressionPlan.GetExpressions())
        {
            CollectAggregateFunctions(child, aggregateFunctions);
        }
    }

    private static bool ContainsAggregateFunction(Expression expression)
    {
        if (expression is FunctionCallExpression functionCall && functionCall.Binding?.GetState<SqlFunction>() is AggregateFunction)
        {
            return true;
        }

        foreach (var child in expression.GetChildren())
        {
            if (child is Expression childExpression && ContainsAggregateFunction(childExpression))
            {
                return true;
            }
        }

        return false;
    }

    private static object? EvaluateExpression(ExpressionPlan.ExpressionPlanNode expressionPlan, RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues)
        => expressionPlan.Execute(row, aggregateValues);

    private static string GetFieldName(Expression expression)
        => expression switch
        {
            IdentifierExpression identifier => identifier.Column,
            FunctionCallExpression functionCall => functionCall.FunctionName,
            LiteralExpression literal => literal.Value?.ToString() ?? "Literal",
            _ => expression.GetType().Name,
        };

    private static object? GetValue(RowAccessor row, string? tableName, string columnName)
        => ExpressionPlan.ExpressionPlanNode.NormalizeValue(ExpressionPlan.ExpressionPlanNode.GetValue(row, tableName, columnName));

}
