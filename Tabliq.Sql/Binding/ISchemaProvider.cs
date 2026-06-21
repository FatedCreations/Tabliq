using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tabliq.Sql.Binding;

public interface ISchemaProvider
{
    // synchronous for now; could be Task-based later
    TableSymbol? GetTable(string name);
    ParameterSymbol? GetParameter(string name);
    FunctionSymbol? GetFunction(string name);
}

public sealed record TableSymbol(string Name, IReadOnlyList<ColumnSymbol> Columns, bool IsLocal = false)
{
    public object? State { get; init; }
}

public sealed record ColumnSymbol(
    string Name,
    string Type,
    bool IsLocal = false)
{
    public object? State { get; init; }
}

public sealed record ParameterSymbol(string Name, string Type, bool IsLocal = false)
{
    public object? State { get; init; }
}

public sealed record FunctionSymbol(string Name,
    IReadOnlyList<FunctionArgumentSymbol> Arguments,
    FunctionArgumentSymbol? ParamsArgument = null, // for additional params, like in a variadic function (i.e. Concat)
    bool IsLocal = false)
{
    public object? State { get; init; }
}

public sealed record FunctionArgumentSymbol(string Name, Type? RequiredType = null, BinderHandling BinderHandling = BinderHandling.Bind, bool Optional = false)
{
    public object? State { get; init; }
}

public enum BinderHandling
{
    Bind = 0,
    Skip = 1,
}