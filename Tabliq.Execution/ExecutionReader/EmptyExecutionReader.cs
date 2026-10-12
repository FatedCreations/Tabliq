using System;
using System.Collections.Generic;
using System.Text;

namespace Tabliq.Execution.ExecutionReader;

public class EmptyExecutionReader : BaseExecutionReader
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


    public override ReadOnlySpan<string> GetFields()
    {
        return _fields;
    }

    public override ReadOnlySpan<object?> GetValues()
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(false);
}
