using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Printer;

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
    private ExecutionPlanNode? RewriteProjection(ExecutionPlanNode node)
    {
        if (node is not ProjectionExecutionPlanNode projection || projection.SourceSelect is null || projection.Input.Provider != this)
        {
            return null;
        }

        return new RemoteSqProviderSqlExecutionPlanNode(this, projection.SourceSelect);
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