using System.Collections.Generic;
using System.Threading.Tasks;
using Tabliq.Sql.Ast;

namespace Tabliq.Sql.Binding;

public interface ISchemaProvider
{
    // synchronous for now; could be Task-based later
    TableSymbol? GetTable(string tableName, string? schemaName = null);
    ParameterSymbol? GetParameter(string name);
    FunctionSymbol? GetFunction(string name);
}


public sealed class TableSymbol
{
    public TableSymbol(string TableName, string? SchemaName, IReadOnlyList<ColumnSymbol> Columns)
    {
        this.SchemaName = SchemaName ?? string.Empty;
        this.TableName = TableName;
        this.Columns = Columns;
    }

    public TableSymbol(string TableName, IReadOnlyList<ColumnSymbol> Columns)
        : this(TableName, string.Empty, Columns)
    {
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public IReadOnlyList<ColumnSymbol> Columns { get; }
    private Dictionary<Type, object?>? _state = null;

    public T GetOrCreateState<T>(Func<T> create)
    {
        if (_state is null || !_state.TryGetValue(typeof(T), out var state))
        {
            state = create();
            _state ??= new Dictionary<Type, object?>();
            _state[typeof(T)] = state;
        }
        return (T)state!;
    }
    public T? GetState<T>()
    {
        if (_state is null)
        {
            return default;
        }

        if (_state.TryGetValue(typeof(T), out var state))
        {
            return (T)state!;
        }

        return default;
    }
    public TableSymbol WithState<T>(T state)
    {
        _state ??= new Dictionary<Type, object?>();
        _state[typeof(T)] = state;
        return this;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(SchemaName)
            ? $"{TableName}"
            : $"{SchemaName}.{TableName}";
    }

    public bool IsMatch(string tableName, string? schemaName)
    {
        return TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)
            && SchemaName.Equals(schemaName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ColumnSymbol(
    string Name,
    string Type)
{
    public bool ExcludeFromStarExpansion { get; init; } = false;

    private Dictionary<Type, object?>? _state = null;

    public T? GetState<T>()
    {
        if (_state is null)
        {
            return default;
        }

        if (_state.TryGetValue(typeof(T), out var state))
        {
            return (T)state!;
        }

        return default;
    }
    public ColumnSymbol WithState<T>(T state)
    {
        _state ??= new Dictionary<Type, object?>();
        _state[typeof(T)] = state;
        return this;
    }
}

public sealed record ParameterSymbol(string Name, string Type, bool IsLocal = false)
{
    public object? State { get; init; }
}

public sealed record FunctionSymbol(
    string Name,
    bool IsAggregate,
    IReadOnlyList<FunctionArgumentSymbol> Arguments,
    FunctionArgumentSymbol? ParamsArgument = null) // for additional params, like in a variadic function (i.e. Concat))
{
    private Dictionary<Type, object?>? _state = null;

    public T? GetState<T>()
    {
        if (_state is null)
        {
            return default;
        }

        if (_state.TryGetValue(typeof(T), out var state))
        {
            return (T)state!;
        }

        return default;
    }
    public FunctionSymbol WithState<T>(T state)
    {
        _state ??= new Dictionary<Type, object?>();
        _state[typeof(T)] = state;
        return this;
    }
}

public sealed record FunctionArgumentSymbol(string Name, Type? RequiredType = null, BinderHandling BinderHandling = BinderHandling.Bind, bool Optional = false)
{
    private Dictionary<Type, object?>? _state = null;

    public T? GetState<T>()
    {
        if (_state is null)
        {
            return default;
        }

        if (_state.TryGetValue(typeof(T), out var state))
        {
            return (T)state!;
        }

        return default;
    }
    public FunctionArgumentSymbol WithState<T>(T state)
    {
        _state ??= new Dictionary<Type, object?>();
        _state[typeof(T)] = state;
        return this;
    }
}

public enum BinderHandling
{
    Bind = 0,
    Skip = 1,
}

public sealed record CteTableMetadata(string Alias, SelectExpression Body, int DeclarationOrder);