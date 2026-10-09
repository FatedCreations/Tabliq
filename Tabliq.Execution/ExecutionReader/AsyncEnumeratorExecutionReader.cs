namespace Tabliq.Execution.ExecutionReader;

public class AsyncEnumeratorExecutionReader : BaseExecutionReader
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

    public override async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            await disposable.DisposeAsync();
        }
    }

    public override ReadOnlySpan<string> GetFields()
    {
        return _fields;
    }

    public override ReadOnlySpan<object?> GetValues()
    {
        return _enumerator.Current;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var result = await _enumerator.MoveNextAsync();
        return result;
    }
}
