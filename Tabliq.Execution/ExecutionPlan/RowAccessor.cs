namespace Tabliq.Execution;

public ref struct RowAccessor
{
    public static RowAccessor Empty => new RowAccessor(ReadOnlySpan<string?>.Empty, ReadOnlySpan<object?>.Empty);

    private readonly ReadOnlySpan<string?> _columns;

    private readonly ReadOnlySpan<object?> _row;

    public RowAccessor(ReadOnlySpan<string?> columns, ReadOnlySpan<object?> row)
    {
        _columns = columns;
        _row = row;
    }

    public RowAccessor(IReadOnlyDictionary<string, object?> rowDict)
    {
        _columns = rowDict.Keys.ToArray();
        _row = rowDict.Values.ToArray();
    }

    public ReadOnlySpan<string?> Columns => _columns;

    public ReadOnlySpan<object?> Values => _row;

    public object? this[string columnName]
    {
        get
        {
            if (TryGetValue(columnName, out var val))
            {
                return val;
            }
            return null;
        }
    }
    public object? this[int columnIndex]
    {
        get
        {
            if (columnIndex >= 0 && columnIndex < _row.Length)
            {
                return _row[columnIndex];
            }
            return null;
        }
    }

    public bool TryGetValue(string columnName, out object? value)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (string.Equals(_columns[i], columnName, StringComparison.OrdinalIgnoreCase))
            {
                value = _row[i];
                return true;
            }
        }
        value = null;
        return false;
    }
    public bool TryGetValue(int columnIndex, out object? value)
    {
        if (columnIndex >= 0 && columnIndex < _row.Length)
        {
            value = _row[columnIndex];
            return true;
        }
        value = null;
        return false;
    }
}