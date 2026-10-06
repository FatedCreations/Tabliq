using System.Diagnostics.CodeAnalysis;
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
                res = new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], projection.SourceSelect));
                return true;
            }
        }

        if (node is ProjectionExecutionPlanNode projectionNode && projectionNode.SourceSelect is not null
            && (projectionNode.Input is EmptyExecutionPlanNode || projectionNode.Input is FilterExecutionPlanNode { Input: EmptyExecutionPlanNode }))
        {
            res = new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], projectionNode.SourceSelect));
            return true;
        }

        res = null;
        return false;
    }

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node, ExecutionRewriteContext? context = null)
        => RewriteJoinsBothSideSql(node, context) ??
            RewriteFilter(node, context) ??
            RewriteProjection(node, context) ??
            RewriteTableScan(node, context)
            ?? node;

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

        var select = new SelectStatement(parentQuery.Sql.CommonTableExpressions, sql);
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

        context?.Report("SqlPushdownApplied", "Projection was pushed to SQL provider.", ExecutionRewriteDiagnosticLevel.Debug, nameof(ProjectionExecutionPlanNode), projection.SourceSelect.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], projection.SourceSelect));
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
    }
}