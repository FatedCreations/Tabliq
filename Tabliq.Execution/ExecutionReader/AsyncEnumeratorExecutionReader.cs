namespace Tabliq.Execution.ExecutionReader;

public class AsyncEnumeratorExecutionReader : IExecutionReader
{
    private readonly IAsyncDisposable[] _disposables;
    private readonly string[] _fields;
    private readonly IAsyncEnumerator<object?[]?> _enumerator;

    public AsyncEnumeratorExecutionReader(string[] fields, IAsyncEnumerator<object?[]?> enumerator, IEnumerable<IAsyncDisposable> disposables)
    {
        _fields = fields;
        _enumerator = enumerator;
        _disposables = disposables.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            await disposable.DisposeAsync();
        }
    }

    public ReadOnlySpan<string> GetFields()
    {
        return _fields;
    }

    public ReadOnlySpan<object?> GetValues()
    {
        return _enumerator.Current;
    }

    public async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var result = await _enumerator.MoveNextAsync();
        return result;
    }
}
