using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Providers;

public class InMemoryProvider : IExecutionProvider
{
    private readonly string[] _columns;
    private readonly IEnumerable<object?[]> _rows;
    private readonly TableSymbol _table;

    public InMemoryProvider(string tableName, string? schemaName, IEnumerable<string> columns, IEnumerable<object?[]> rows)
    {
        _columns = columns.ToArray();
        _rows = rows;
        _table = new TableSymbol(tableName, schemaName, _columns.Select(c => new ColumnSymbol(c, string.Empty)).ToArray())
        {
            State = this
        };
    }
    public Task<IExecutionReader> ReadTableAsync(string tableName, string? schemaName = null, CancellationToken cancellationToken = default)
    {
        if (!_table.IsMatch(tableName, schemaName))
        {
            throw new InvalidOperationException($"Table '{tableName}' not found.");
        }

        return Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(_columns, _rows.GetEnumerator(), Array.Empty<IAsyncDisposable>()));
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
}

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
        {
            State = this
        };
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

    public Task<IExecutionReader> ReadTableAsync(string tableName, string? schemaName = null, CancellationToken cancellationToken = default)
    {
        if (!_table.IsMatch(tableName, schemaName))
        {
            throw new InvalidOperationException($"Table '{tableName}' not found.");
        }

        return Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(_columns, Rows().GetEnumerator(), Array.Empty<IAsyncDisposable>()));
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
}
