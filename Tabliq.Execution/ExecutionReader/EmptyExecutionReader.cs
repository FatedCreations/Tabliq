using System;
using System.Collections.Generic;
using System.Text;

namespace Tabliq.Execution.ExecutionReader;

public class EmptyExecutionReader : IExecutionReader
{
    private readonly string[] _fields;

    public EmptyExecutionReader(string[] fields)
    {
        _fields = fields;
    }
    public EmptyExecutionReader()
    {
        _fields = Array.Empty<string>();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ReadOnlySpan<string> GetFields()
    {
        return _fields;
    }

    public ReadOnlySpan<object?> GetValues()
    {
        throw new NotImplementedException();
    }
    public RowAccessor GetRowAccessor()
        => new RowAccessor(GetFields(), GetValues());

    public Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(false);
}
