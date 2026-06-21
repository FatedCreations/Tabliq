using System.Collections.Generic;
using System.Linq;
using Tabliq.Sql.Binding;

namespace Tabliq.Tests.Sql;

public sealed class SampleCatalog : ISchemaProvider
{
    private readonly List<TableSymbol> _tables = new();
    private readonly List<ParameterSymbol> _parameters = new();
    private readonly List<FunctionSymbol> _functions = new();

    public SampleCatalog()
    {
        _tables.Add(new TableSymbol("Users", new List<ColumnSymbol>
        {
            new ColumnSymbol("Id", "int"),
            new ColumnSymbol("Name", "string"),
            new ColumnSymbol("Age", "int"),
        }));

        _tables.Add(new TableSymbol("Orders", new List<ColumnSymbol>
        {
            new ColumnSymbol("Id", "int"),
            new ColumnSymbol("UserId", "int"),
            new ColumnSymbol("Total", "decimal"),
        }));
    }

    public ParameterSymbol? GetParameter(string name) => _parameters.FirstOrDefault(p => p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

    public TableSymbol? GetTable(string name) => _tables.FirstOrDefault(t => t.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

    public FunctionSymbol? GetFunction(string name) => _functions.FirstOrDefault(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
}
