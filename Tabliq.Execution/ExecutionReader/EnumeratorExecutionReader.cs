using System;
using System.Collections.Generic;
using System.Text;

namespace Tabliq.Execution.ExecutionReader;

public class EnumeratorExecutionReader : IExecutionReader
{
    private readonly IAsyncDisposable[] _disposables;
    private readonly string[] _fields;
    private readonly IEnumerator<object?[]?> _enumerator;

    public EnumeratorExecutionReader(string[] fields, IEnumerator<object?[]?> enumerator, IEnumerable<IAsyncDisposable> disposables)
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

    public Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var result = _enumerator.MoveNext();
        return Task.FromResult(result);
    }
}