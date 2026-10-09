using System.Buffers;

namespace Tabliq.Execution.ExecutionReader;

public class NullRowExecutionReader : BaseExecutionReader
{
    private readonly string[] _fields;
    private readonly object?[] _data;

    private bool _hasRead = false;

    public NullRowExecutionReader(string[] fields)
    {
        _fields = fields;
        _data = ArrayPool<object?>.Shared.Rent(fields.Length);
    }

    public NullRowExecutionReader()
        : this(Array.Empty<string>())
    {
    }

    public override async ValueTask DisposeAsync()
    {
        ArrayPool<object?>.Shared.Return(_data);
    }

    public override ReadOnlySpan<string> GetFields()
    {
        return _fields;
    }

    public override ReadOnlySpan<object?> GetValues()
        => _data.AsSpan(0, _fields.Length);

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        // can read 1 row, then return false on subsequent calls
        if (!_hasRead)
        {
            _hasRead = true;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
