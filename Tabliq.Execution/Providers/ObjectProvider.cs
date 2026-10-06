using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Providers;

public static class ObjectProvider
{
    public static ObjectProvider<T> Create<T>(string tableName, string? schemaName, IEnumerable<T> rows)
        => new ObjectProvider<T>(tableName, schemaName, rows);
}

public class ObjectProvider<T> : IExecutionProvider
{
    private readonly IEnumerable<T> _rows;
    private readonly string[] _columns;
    private readonly TableSymbol _table;

    public ObjectProvider(string tableName, string? schemaName, IEnumerable<T> rows)
    {
        _rows = rows;
        _columns = typeof(T).GetProperties().Select(f => f.Name).ToArray();

        _table = new TableSymbol(tableName, schemaName, _columns.Select(c => new ColumnSymbol(c, string.Empty)).ToArray())
            .WithState(this);
    }
    public ObjectProvider(string tableName, IEnumerable<T> rows)
        : this(tableName, null, rows)
    {
    }

    private IEnumerable<object?[]> Rows()
    {
        var fields = typeof(T).GetProperties();
        object?[] row = new object?[fields.Length];

        foreach (var r in _rows)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                row[i] = fields[i].GetValue(r);
            }

            yield return row;
        }
    }

    private Task<IExecutionReader> ReadTableAsync(string tableName, string? schemaName = null, string? alias = null, CancellationToken cancellationToken = default)
    {
        if (!_table.IsMatch(tableName, schemaName))
        {
            throw new InvalidOperationException($"Table '{tableName}' not found.");
        }

        alias ??= tableName;
        var cols = _columns.Select(x => $"{alias}.{x}").ToArray();

        return Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(cols, Rows().GetEnumerator(), Array.Empty<IAsyncDisposable>()));
    }

    public FunctionSymbol? GetFunction(string functionName)
        => null;

    public TableSymbol? GetTable(string tableName, string? schemaName = null)
    {
        if (_table.IsMatch(tableName, schemaName))
        {
            return _table;
        }
        return null;
    }

    public IEnumerable<TableSymbol> GetTables()
    {
        yield return _table;
    }

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node, ExecutionRewriteContext? context = null)
    {
        if (node is TableScanExecutionPlanNode tableScan)
        {
            if (_table.IsMatch(tableScan.TableName, tableScan.SchemaName))
            {
                context?.Report("ObjectTableScanRewritten", "Table scan was rewritten to an in-memory object provider path.", ExecutionRewriteDiagnosticLevel.Debug, nameof(TableScanExecutionPlanNode), tableScan.TableName);
                return new ObjectProviderTableScanNode(this, tableScan.TableName, tableScan.SchemaName, tableScan.Alias);
            }
        }

        return node;
    }

    private class ObjectProviderTableScanNode : ExecutionPlanNode
    {
        private readonly ObjectProvider<T> _provider;
        private readonly string _tableName;
        private readonly string? _schemaName;
        private readonly string _alias;

        public ObjectProviderTableScanNode(ObjectProvider<T> provider, string tableName, string? schemaName = null, string? alias = null)
        {
            _provider = provider;
            _tableName = tableName;
            _schemaName = schemaName;
            _alias = alias ?? _tableName;
        }

        public override IExecutionProvider? Provider => _provider;

        public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            return _provider.ReadTableAsync(_tableName, _schemaName, _alias, cancellationToken);
        }

        public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null) => null;
    }
}
