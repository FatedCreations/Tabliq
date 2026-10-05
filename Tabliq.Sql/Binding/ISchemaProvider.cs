using System.Collections.Generic;
using System.Threading.Tasks;

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
    public object? State { get; init; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(SchemaName)
            ? $"{TableName}"
            : $"{SchemaName}.{TableName}";
    }


}

public sealed record ColumnSymbol(
    string Name,
    string Type)
{
    public object? State { get; init; }
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