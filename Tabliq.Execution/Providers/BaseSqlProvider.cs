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

        res = null;
        return false;
    }

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node)
        => RewriteJoinsBothSideSql(node) ??
            RewriteFilter(node) ??
            RewriteProjection(node) ??
            RewriteTableScan(node)
            ?? node;

    private ExecutionPlanNode? RewriteTableScan(ExecutionPlanNode node)
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
    private ExecutionPlanNode? RewriteFilter(ExecutionPlanNode node)
    {
        if (node is not FilterExecutionPlanNode filter || !TryGetParentQuery(filter.Input, out var parentQuery))
        {
            return null;
        }

        if (ContainsUnsupportedFunction(filter.Condition))
        {
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

    private ExecutionPlanNode? RewriteProjection(ExecutionPlanNode node)
    {
        if (node is not ProjectionExecutionPlanNode projection || projection.SourceSelect is null || projection.Input.Provider != this)
        {
            return null;
        }

        if (ContainsUnsupportedFunction(projection.SourceSelect))
        {
            var pushedDownSelect = TryCreatePushdownSelect(projection.SourceSelect);
            if (pushedDownSelect is not null)
            {
                return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], pushedDownSelect));
            }

            return null;
        }

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
        if (expression is FunctionCallExpression functionCall && functionCall.Binding is not null && !SupportsFunction(functionCall.Binding))
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
    {
        if (node is FunctionCallExpression functionCall && functionCall.Binding is not null && !SupportsFunction(functionCall.Binding))
        {
            return true;
        }

        foreach (var child in node.GetChildren())
        {
            if (ContainsUnsupportedFunction(child))
            {
                return true;
            }
        }

        return false;
    }

    protected virtual bool SupportsFunction(FunctionSymbol function)
    {
        if (function.GetState<SqlFunction>() is not SqlFunction sqlFunction)
        {
            return true;
        }

        return sqlFunction.Name.Equals("COUNT", StringComparison.OrdinalIgnoreCase);
    }

    private ExecutionPlanNode? RewriteJoinsBothSideSql(ExecutionPlanNode node)
    {
        // we might be able to special case we one side is not sql and convert it to the `select foo in () pattern`
        if (node is not JoinExecutionPlanNode join || !TryGetParentQuery(join.Left, out var left) || !TryGetParentQuery(join.Right, out var right))
        {
            return null;
        }

        // merge the results for the 2 sql results! they already shoudl be aliased!
        var trefs = left.Sql.SelectQuery.From!.TableReferences;
        var joins = left.Sql.SelectQuery.From.Joins.Append(new JoinClause(join.JoinSide, join.JoinType, right.Sql.SelectQuery.From!.TableReferences.Single(), join.Condition));

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

        public override ExecutionPlanNode? TryRewrite() => null;
    }
}