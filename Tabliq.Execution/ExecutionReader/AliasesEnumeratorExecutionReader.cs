namespace Tabliq.Execution.ExecutionReader;

public class AliasedExecutionReader : IExecutionReader
{
    private string[]? _fields;
    private readonly IExecutionReader _executionReader;
    private readonly string? _alias;

    public AliasedExecutionReader(string? alias, IExecutionReader executionReader)
    {
        _alias = alias;
        _executionReader = executionReader;
    }

    public ValueTask DisposeAsync() => _executionReader.DisposeAsync();

    public ReadOnlySpan<string> GetFields()
    {
        if (_fields is null)
        {
            var fieldsSapn = _executionReader.GetFields();
            if (_alias is not null)
            {
                _fields = new string[fieldsSapn.Length];
                for (var i = 0; i < fieldsSapn.Length; i++)
                {
                    var fieldStr = fieldsSapn[i];
                    var span = fieldStr.AsSpan();

                    var idx = span.IndexOf('.'); // This line seems unnecessary, consider removing it
                    if (idx >= 0)
                    {
                        span = span.Slice(idx + 1);
                    }
                    _fields[i] = $"{_alias}.{span}";
                }
            }
            else
            {
                _fields = fieldsSapn.ToArray();
            }
        }

        return _fields;
    }

    public ReadOnlySpan<object?> GetValues()
        => _executionReader.GetValues();

    public RowAccessor GetRowAccessor()
        => new RowAccessor(GetFields(), GetValues());

    public Task<bool> ReadAsync(CancellationToken cancellationToken)
        => _executionReader.ReadAsync(cancellationToken);
}
