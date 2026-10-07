using System.Diagnostics.CodeAnalysis;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Execution.Providers;

public abstract class RemoteSqlProviderBase : IExecutionProvider
{
    public virtual TableSymbol? GetTable(string tableName, string? schemaName = null)
        => GetTables().FirstOrDefault(x => x.IsMatch(tableName, schemaName));

    public abstract IEnumerable<TableSymbol> GetTables();

    private bool TryGetParentQuery(ExecutionPlanNode node, [NotNullWhen(true)] out RemoteSqProviderSqlExecutionPlanNode? res)
    {
        if (node.Provider == this && node is RemoteSqProviderSqlExecutionPlanNode n)
        {
            res = n;
            return true;
        }

        if (node is SubqueryExecutionPlanNode subquery)
        {
            if (TryGetParentQuery(subquery.Inner, out res))
            {
                return true;
            }

            if (subquery.Inner is ProjectionExecutionPlanNode projection && projection.SourceSelect is not null)
            {
                var cteSource = ResolveCteSource(subquery, projection.SourceSelect);
                var ctes = MergeCteDefinitions(
                    CollectCteDefinitions(cteSource, new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                    subquery.IsCte && !string.IsNullOrEmpty(subquery.CteName) && subquery.CteBody is not null
                        ? [new CommonTableExpression(subquery.CteName, subquery.CteBody)]
                        : Enumerable.Empty<CommonTableExpression>());

                var cteSql = new SelectStatement(ctes, projection.SourceSelect);
                res = new RemoteSqProviderSqlExecutionPlanNode(this, cteSql);
                return true;
            }
        }

        if (node is FilterExecutionPlanNode filter && TryGetParentQuery(filter.Input, out res))
        {
            return true;
        }

        if (node is JoinExecutionPlanNode join)
        {
            if (TryGetParentQuery(join.Left, out res))
            {
                return true;
            }

            if (TryGetParentQuery(join.Right, out res))
            {
                return true;
            }
        }

        if (node is ProjectionExecutionPlanNode projectionNode && projectionNode.SourceSelect is not null
            && (projectionNode.Input is EmptyExecutionPlanNode || projectionNode.Input is FilterExecutionPlanNode { Input: EmptyExecutionPlanNode }))
        {
            var sourceCtes = CollectCteDefinitions(projectionNode.SourceSelect, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            res = new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement(sourceCtes, projectionNode.SourceSelect));
            return true;
        }

        res = null;
        return false;
    }

    private static IReadOnlyList<CommonTableExpression> CollectCteDefinitions(SyntaxNode node, HashSet<string>? seen = null)
    {
        seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ctes = new List<CommonTableExpression>();

        void Visit(SyntaxNode current)
        {
            if (current is SelectExpression nestedSelect && !ReferenceEquals(current, node))
            {
                foreach (var nested in CollectCteDefinitions(nestedSelect, seen))
                {
                    if (seen.Add(nested.Alias))
                    {
                        ctes.Add(nested);
                    }
                }
            }

            if (current is NamedTableReference namedTable && namedTable.Binding is not null)
            {
                if (namedTable.Binding.GetState<CteTableMetadata>() is CteTableMetadata cte && seen.Add(cte.Alias))
                {
                    Visit(cte.Body);
                    ctes.Add(new CommonTableExpression(cte.Alias, cte.Body));
                }
            }

            foreach (var child in current.GetChildren())
            {
                Visit(child);
            }
        }

        Visit(node);
        return ctes;
    }

    private static IEnumerable<CommonTableExpression> MergeCteDefinitions(params IEnumerable<CommonTableExpression>[] cteSets)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cte in cteSets.SelectMany(set => set))
        {
            if (seen.Add(cte.Alias))
            {
                yield return cte;
            }
        }
    }

    private static SyntaxNode ResolveCteSource(SubqueryExecutionPlanNode subquery, SyntaxNode fallback)
        => subquery.CteBody ?? fallback;

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node, ExecutionRewriteContext? context = null)
        => RewriteUnion(node, context) ??
            RewriteJoinsBothSideSql(node, context) ??
            RewriteFilter(node, context) ??
            RewriteProjection(node, context) ??
            RewriteTableScan(node, context)
            ?? node;

    private ExecutionPlanNode? RewriteUnion(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not UnionExecutionPlanNode union)
        {
            return null;
        }

        var branchQueries = new List<(SelectStatement Sql, bool IsAll)>();
        foreach (var operation in union.Operations)
        {
            if (!TryGetParentQuery(operation.Input, out var parentQuery))
            {
                return null;
            }

            branchQueries.Add((parentQuery.Sql, operation.IsAll));
        }

        if (branchQueries.Count == 0)
        {
            return null;
        }

        var first = branchQueries[0].Sql;
        var unionStatements = new List<UnionStatement>();
        foreach (var (sql, isAll) in branchQueries.Skip(1))
        {
            unionStatements.Add(new UnionStatement(isAll, sql.SelectQuery));
        }

        var mergedCtes = new List<CommonTableExpression>();
        foreach (var (sql, _) in branchQueries)
        {
            mergedCtes.AddRange(sql.CommonTableExpressions);
        }

        var select = new SelectExpression(
            first.SelectQuery.IsBracketed,
            first.SelectQuery.Top,
            first.SelectQuery.Distinctness,
            first.SelectQuery.Projections,
            first.SelectQuery.From,
            first.SelectQuery.Where,
            first.SelectQuery.GroupBy,
            first.SelectQuery.Having,
            first.SelectQuery.OrderBy,
            unionStatements);

        var result = new SelectStatement(mergedCtes.DistinctBy(x => x.Alias, StringComparer.OrdinalIgnoreCase), select);
        context?.Report("SqlPushdownApplied", "Union inputs were merged into a single SQL query.", ExecutionRewriteDiagnosticLevel.Debug, nameof(UnionExecutionPlanNode), union.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, result);
    }

    private ExecutionPlanNode? RewriteTableScan(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not TableScanExecutionPlanNode tableScan)
        {
            return null;
        }
        var tables = GetTables();
        var table = tables.FirstOrDefault(x => x.TableName == tableScan.TableName && x.SchemaName == tableScan.SchemaName)
            ?? tables.FirstOrDefault(x => x.TableName == tableScan.TableName);

        if (table is null)
        {
            return null;
        }

        IEnumerable<ColumnSymbol> columns = tableScan.ReferencedColumns is { Count: > 0 }
            ? tableScan.ReferencedColumns
            : table.Columns;

        var sql = new SelectExpression(
            false,
            null,
            Distinctness.Unspecified,
            columns.Select(x => new SelectProjection(new IdentifierExpression(tableScan.Alias, x.Name))),
            new FromClause([new NamedTableReference(IdentifierExpression.FromTableSymbol(table), tableScan.Alias)], []),
            null,
            null,
            null,
            null,
            []);

        return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], sql));
    }
    private ExecutionPlanNode? RewriteFilter(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not FilterExecutionPlanNode filter || !TryGetParentQuery(filter.Input, out var parentQuery))
        {
            return null;
        }

        var ctes = parentQuery.Sql.CommonTableExpressions;
        if (filter.Input is SubqueryExecutionPlanNode filterSubquery)
        {
            var seen = new HashSet<string>(parentQuery.Sql.CommonTableExpressions.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase);
            var nestedCtes = CollectCteDefinitions(ResolveCteSource(filterSubquery, parentQuery.Sql.SelectQuery), seen);
            var currentCte = filterSubquery.IsCte && !string.IsNullOrEmpty(filterSubquery.CteName) && filterSubquery.CteBody is not null
                ? [new CommonTableExpression(filterSubquery.CteName, filterSubquery.CteBody)]
                : Enumerable.Empty<CommonTableExpression>();
            ctes = MergeCteDefinitions(parentQuery.Sql.CommonTableExpressions, nestedCtes, currentCte).ToList();
        }

        var nestedExpressionCtes = CollectCteDefinitions(filter.Condition, new HashSet<string>(ctes.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase));
        ctes = MergeCteDefinitions(ctes, nestedExpressionCtes).ToList();

        if (ContainsUnsupportedFunction(filter.Condition))
        {
            context?.Report("SqlPushdownSkipped", "Filter could not be pushed to SQL because it contains an unsupported function.", ExecutionRewriteDiagnosticLevel.Warning, nameof(FilterExecutionPlanNode), filter.Condition.ToString());
            return null;
        }

        var where = parentQuery.Sql.SelectQuery.Where is null
            ? new WhereClause(filter.Condition)
            : new WhereClause(new LogicalCondition(parentQuery.Sql.SelectQuery.Where.Condition, LogicalOperator.And, filter.Condition));

        var sql = new SelectExpression(
            parentQuery.Sql.SelectQuery.IsBracketed,
            parentQuery.Sql.SelectQuery.Top,
            parentQuery.Sql.SelectQuery.Distinctness,
            parentQuery.Sql.SelectQuery.Projections,
            parentQuery.Sql.SelectQuery.From,
            where,
            parentQuery.Sql.SelectQuery.GroupBy,
            parentQuery.Sql.SelectQuery.Having,
            parentQuery.Sql.SelectQuery.OrderBy,
            parentQuery.Sql.SelectQuery.UnionStatements);

        var select = new SelectStatement(ctes.Distinct(), sql);
        return new RemoteSqProviderSqlExecutionPlanNode(this, select);
    }

    private ExecutionPlanNode? RewriteProjection(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not ProjectionExecutionPlanNode projection || projection.SourceSelect is null || projection.Input.Provider != this)
        {
            return null;
        }

        var unsupportedFunctions = GetUnsupportedFunctionCalls(projection.SourceSelect).ToList();
        if (unsupportedFunctions.Count > 0)
        {
            foreach (var unsupportedFunction in unsupportedFunctions)
            {
                context?.Report(
                    unsupportedFunction.Binding is not null ? $"SqlPushdownUnsupportedFunction:{unsupportedFunction.FunctionName}" : "SqlPushdownUnsupportedFunction",
                    $"Projection depends on unsupported function '{unsupportedFunction.FunctionName}' and cannot be fully pushed to SQL.",
                    ExecutionRewriteDiagnosticLevel.Warning,
                    nameof(ProjectionExecutionPlanNode),
                    unsupportedFunction.ToString());
            }

            var pushedDownSelect = TryCreatePushdownSelect(projection.SourceSelect);
            if (pushedDownSelect is not null)
            {
                context?.Report(
                    "SqlPushdownPartial",
                    $"Projection partially pushed to SQL; unsupported function(s) will be evaluated in memory.",
                    ExecutionRewriteDiagnosticLevel.Info,
                    nameof(ProjectionExecutionPlanNode),
                    projection.SourceSelect.ToString());
                var pushedDownInput = new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], pushedDownSelect));


                return new ProjectionExecutionPlanNode(pushedDownInput, projection.Projections, pushedDownSelect);
            }

            context?.Report(
                "SqlPushdownSkipped",
                "Projection could not be pushed to SQL because it depends on unsupported function(s).",
                ExecutionRewriteDiagnosticLevel.Warning,
                nameof(ProjectionExecutionPlanNode),
                projection.SourceSelect.ToString());
            return null;
        }

        IEnumerable<CommonTableExpression> ctes = projection.Input is SubqueryExecutionPlanNode projectionSubquery
            ? MergeCteDefinitions(
                CollectCteDefinitions(ResolveCteSource(projectionSubquery, projection.SourceSelect), new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                projectionSubquery.IsCte && !string.IsNullOrEmpty(projectionSubquery.CteName) && projectionSubquery.CteBody is not null
                    ? [new CommonTableExpression(projectionSubquery.CteName, projectionSubquery.CteBody)]
                    : Enumerable.Empty<CommonTableExpression>())
            : TryGetParentQuery(projection.Input, out var parentQuery)
                ? MergeCteDefinitions(
                    parentQuery.Sql.CommonTableExpressions,
                    CollectCteDefinitions(projection.SourceSelect, new HashSet<string>(parentQuery.Sql.CommonTableExpressions.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase)))
                : CollectCteDefinitions(projection.SourceSelect, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var projectionExpressionCtes = projection.Projections
            .SelectMany(projectionItem => CollectCteDefinitions(
                projectionItem.Expression,
                new HashSet<string>(ctes.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase)))
            .ToList();
        ctes = MergeCteDefinitions(ctes, projectionExpressionCtes).ToList();

        var nestedExpressionCtes = CollectCteDefinitions(projection.SourceSelect, new HashSet<string>(ctes.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase));
        ctes = MergeCteDefinitions(ctes, nestedExpressionCtes).ToList();

        context?.Report("SqlPushdownApplied", "Projection was pushed to SQL provider.", ExecutionRewriteDiagnosticLevel.Debug, nameof(ProjectionExecutionPlanNode), projection.SourceSelect.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement(ctes, projection.SourceSelect));
    }

    private SelectExpression? TryCreatePushdownSelect(SelectExpression source)
    {
        var projections = new List<SelectProjection>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var projection in source.Projections)
        {
            if (ContainsUnsupportedFunction(projection.Expression))
            {
                foreach (var dependent in CollectPushdownDependencies(projection.Expression))
                {
                    var key = dependent.Expression.ToString();
                    if (seen.Add(key))
                    {
                        projections.Add(dependent);
                    }
                }

                continue;
            }

            var key2 = projection.Expression.ToString();
            if (seen.Add(key2))
            {
                projections.Add(projection);
            }
        }

        if (projections.Count == 0)
        {
            return null;
        }

        var where = source.Where;
        var groupBy = source.GroupBy;
        var having = source.Having;
        var orderBy = source.OrderBy;

       return new SelectExpression(
            source.IsBracketed,
            source.Top,
            source.Distinctness,
            projections,
            source.From,
            where,
            groupBy,
            having,
            orderBy,
            source.UnionStatements);
    }

    private IEnumerable<SelectProjection> CollectPushdownDependencies(Expression expression)
    {
        if (expression is FunctionCallExpression functionCall && functionCall.Binding is not null && !IsFunctionSupportedForPushdown(functionCall))
        {
            foreach (var argument in functionCall.Arguments)
            {
                foreach (var dependency in CollectPushdownDependencies(argument))
                {
                    yield return dependency;
                }
            }

            yield break;
        }

        foreach (var child in expression.GetChildren())
        {
            if (child is Expression childExpression)
            {
                foreach (var dependency in CollectPushdownDependencies(childExpression))
                {
                    yield return dependency;
                }
            }
        }

        if (expression is IdentifierExpression or LiteralExpression or ParameterIdentifier or ValueFromExpression)
        {
            yield return new SelectProjection(expression);
        }
    }

    private bool ContainsUnsupportedFunction(SyntaxNode node)
        => GetUnsupportedFunctionCalls(node).Any();

    protected virtual FunctionCallExpression RewriteFunctionCallForPushdown(FunctionCallExpression functionCall)
        => functionCall;

    protected bool IsFunctionSupportedForPushdown(FunctionCallExpression functionCall)
    {
        var rewrittenCall = RewriteFunctionCallForPushdown(functionCall);
        var function = rewrittenCall.Binding is not null && rewrittenCall.Binding.Name.Equals(rewrittenCall.FunctionName, StringComparison.OrdinalIgnoreCase)
            ? rewrittenCall.Binding
            : new FunctionSymbol(rewrittenCall.FunctionName, rewrittenCall.Binding?.IsAggregate ?? false, rewrittenCall.Binding?.Arguments ?? Array.Empty<FunctionArgumentSymbol>(), rewrittenCall.Binding?.ParamsArgument);
        return SupportsFunction(function);
    }

    private IEnumerable<FunctionCallExpression> GetUnsupportedFunctionCalls(SyntaxNode node)
    {
        if (node is FunctionCallExpression functionCall && functionCall.Binding is not null && !IsFunctionSupportedForPushdown(functionCall))
        {
            yield return functionCall;
        }

        foreach (var child in node.GetChildren())
        {
            foreach (var unsupported in GetUnsupportedFunctionCalls(child))
            {
                yield return unsupported;
            }
        }
    }

    protected virtual bool SupportsFunction(FunctionSymbol function)
    {
        if (function.GetState<SqlFunction>() is not SqlFunction sqlFunction)
        {
            return true;
        }

        return sqlFunction.Name.Equals("COUNT", StringComparison.OrdinalIgnoreCase);
    }

    private ExecutionPlanNode? RewriteJoinsBothSideSql(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        // we might be able to special case we one side is not sql and convert it to the `select foo in () pattern`
        if (node is not JoinExecutionPlanNode join || !TryGetParentQuery(join.Left, out var left) || !TryGetParentQuery(join.Right, out var right))
        {
            return null;
        }

        // merge the results for the 2 sql results! they already shoudl be aliased!
        var leftFrom = left.Sql.SelectQuery.From ?? new FromClause([new SelectTableReference(left.Sql.SelectQuery, "left_subquery")], []);
        var rightFrom = right.Sql.SelectQuery.From ?? new FromClause([new SelectTableReference(right.Sql.SelectQuery, "right_subquery")], []);
        var trefs = leftFrom.TableReferences;
        var rightTableReference = rightFrom.TableReferences.SingleOrDefault() ?? new SelectTableReference(right.Sql.SelectQuery, "right_subquery");
        var joins = leftFrom.Joins.Append(new JoinClause(join.JoinSide, join.JoinType, rightTableReference, join.Condition));

        var sql = new SelectExpression(
            false,
            null,
            Distinctness.Unspecified,
            left.Sql.SelectQuery.Projections.Concat(right.Sql.SelectQuery.Projections),
            new FromClause(trefs, joins),
            null,
            null,
            null,
            null,
            []);

        IEnumerable<CommonTableExpression> allCtes = [.. left.Sql.CommonTableExpressions, .. right.Sql.CommonTableExpressions];
        var select = new SelectStatement(allCtes.Distinct(), sql);

        context?.Report("SqlPushdownApplied", "Join inputs were merged into a single SQL query.", ExecutionRewriteDiagnosticLevel.Debug, nameof(JoinExecutionPlanNode), join.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, select);
    }

    public abstract Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);

    public class TableSymbolWrapper
    {
        public TableSymbol TableSymbol { get; }
        public RemoteSqlProviderBase Provider { get; }
        public TableSymbolWrapper(TableSymbol tableSymbol, RemoteSqlProviderBase provider)
        {
            TableSymbol = tableSymbol;
            Provider = provider;
        }
    }

    public class RemoteSqProviderSqlExecutionPlanNode : ExecutionPlanNode
    {
        private readonly RemoteSqlProviderBase _provider;
        public SelectStatement Sql { get; }

        public RemoteSqProviderSqlExecutionPlanNode(RemoteSqlProviderBase provider, SelectStatement sql)
        {
            _provider = provider;
            Sql = sql;
        }

        public override IExecutionProvider? Provider => _provider;

        public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            return _provider.ExecuteAsync(Sql, parameters, cancellationToken);
        }

        public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null) => null;

        public override IEnumerable<ExecutionPlanNode> GetInputs() => [];
        public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
    }
}