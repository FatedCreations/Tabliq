using System.Diagnostics.CodeAnalysis;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Execution.Providers;

public abstract class RemoteSqlProviderBase : IExecutionProvider
{
    private List<TableSymbol> _tables = new List<TableSymbol>();

    public void AddTable(TableSymbol tableSymbol)
    {
        _tables.Add(new TableSymbol(tableSymbol.TableName, tableSymbol.SchemaName, tableSymbol.Columns)
        {
            State = new TableSymbolWrapper(tableSymbol, this)
        });
    }

    public FunctionSymbol? GetFunction(string functionName) => null;

    public TableSymbol? GetTable(string tableName, string? schemaName = null)
        => _tables.FirstOrDefault(x => x.IsMatch(tableName, schemaName));

    public IEnumerable<TableSymbol> GetTables() => _tables;

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
        var table = _tables.FirstOrDefault(x => x.TableName == tableScan.TableName && x.SchemaName == tableScan.SchemaName)
            ?? _tables.FirstOrDefault(x => x.TableName == tableScan.TableName);

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

        return new RemoteSqProviderSqlExecutionPlanNode(this, sql);
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

        var where = parentQuery.Sql.Where is null
            ? new WhereClause(filter.Condition)
            : new WhereClause(new LogicalCondition(parentQuery.Sql.Where.Condition, LogicalOperator.And, filter.Condition));

        var sql = new SelectExpression(
            parentQuery.Sql.IsBracketed,
            parentQuery.Sql.Top,
            parentQuery.Sql.Distinctness,
            parentQuery.Sql.Projections,
            parentQuery.Sql.From,
            where,
            parentQuery.Sql.GroupBy,
            parentQuery.Sql.Having,
            parentQuery.Sql.OrderBy,
            parentQuery.Sql.UnionStatements);

        return new RemoteSqProviderSqlExecutionPlanNode(this, sql);
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
                return new RemoteSqProviderSqlExecutionPlanNode(this, pushedDownSelect);
            }

            return null;
        }

        return new RemoteSqProviderSqlExecutionPlanNode(this, projection.SourceSelect);
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
        if (function.State is not SqlFunction sqlFunction)
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
        var trefs = left.Sql.From!.TableReferences;
        var joins = left.Sql.From.Joins.Append(new JoinClause(join.JoinSide, join.JoinType, right.Sql.From!.TableReferences.Single(), join.Condition));

        var sql = new SelectExpression(
           false,
           null,
           Distinctness.Unspecified,
           left.Sql.Projections.Concat(right.Sql.Projections),
           new FromClause(trefs, joins),
           null,
           null,
           null,
           null,
           []);

        return new RemoteSqProviderSqlExecutionPlanNode(this, sql);
    }


    // todo need to rebind the select expression to ensure that it is valid for the remote provider
    public abstract Task<IExecutionReader> ExecuteAsync(SelectExpression sqlScript, CancellationToken cancellationToken);

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
        public SelectExpression Sql { get; }

        public RemoteSqProviderSqlExecutionPlanNode(RemoteSqlProviderBase provider, SelectExpression sql)
        {
            _provider = provider;
            Sql = sql;
        }

        public override IExecutionProvider? Provider => _provider;

        public override Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
        {
            return _provider.ExecuteAsync(Sql, cancellationToken);
        }

        public override ExecutionPlanNode? TryRewrite() => null;
    }
}