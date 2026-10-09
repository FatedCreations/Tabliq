using System.Data.Common;

namespace Tabliq.Execution.ExecutionReader;

public class DbReaderExecutionReader : BaseExecutionReader
{
    private string[]? _fields;
    private object?[]? _values;
    private readonly DbDataReader _dbDataReader;
    private readonly IEnumerable<IAsyncDisposable> _disposables;

    public DbReaderExecutionReader(DbDataReader dbDataReader, params IEnumerable<IAsyncDisposable> disposables)
    {
        _dbDataReader = dbDataReader;
        _disposables = disposables;
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
        await _dbDataReader.DisposeAsync().ConfigureAwait(false);
    }

    public override ReadOnlySpan<string> GetFields()
    {
        if (_fields is null)
        {
            _fields = new string[_dbDataReader.FieldCount];
            for (int i = 0; i < _dbDataReader.FieldCount; i++)
            {
                _fields[i] = _dbDataReader.GetName(i);
            }
        }

        return _fields;
    }

    public override ReadOnlySpan<object?> GetValues()
    {
        _values ??= new object?[_dbDataReader.FieldCount];

        _dbDataReader.GetValues(_values!);

        for (var i = 0; i < _values.Length; i++)
        {
            if (_values[i] is DBNull)
            {
                // normalise DBNull to null
                _values[i] = null;
            }
        }

        return _values;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return _dbDataReader.ReadAsync(cancellationToken);
    }
}
