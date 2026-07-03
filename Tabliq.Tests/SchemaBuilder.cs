using Tabliq.Sql.Binding;

namespace Tabliq.Tests;

public class SchemaBuilder
{
    public List<TableBuilder> Tables { get; } = new List<TableBuilder>();
    public List<ParameterSymbol> Parameters { get; } = new List<ParameterSymbol>();

    public TableBuilder AddTable(string name)
    {
        var tbl = Tables.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (tbl is null)
        {
            tbl = new TableBuilder(name, this);
            Tables.Add(tbl);

        }
        return tbl;
    }

    public SchemaBuilder AddParamater(string name, string? type = null)
    {
        var param = Parameters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (param is null)
        {
            param = new ParameterSymbol(name, type ?? string.Empty);
            Parameters.Add(param);

        }
        return this;
    }

    public ISchemaProvider Build()
    {
        return new SimpleSchema(
            Tables.Select(x => x.Build()).ToList(),
            [.. Parameters]);
    }

    public class SimpleSchema : ISchemaProvider
    {
        private readonly List<TableSymbol> _tables;
        private readonly List<ParameterSymbol> _parameters;

        public SimpleSchema(List<TableSymbol> tables, List<ParameterSymbol> parameters)
        {
            _tables = tables;
            _parameters = parameters;
        }

        public FunctionSymbol? GetFunction(string name)
            => null;

        public ParameterSymbol? GetParameter(string name)
            => _parameters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public TableSymbol? GetTable(string name)
            => _tables.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

public class TableBuilder
{
    public List<ColumnSymbol> Columns { get; } = new List<ColumnSymbol>();

    public string Name { get; }

    private readonly SchemaBuilder _schemaBuilder;

    public TableBuilder(string name, SchemaBuilder schemaBuilder)
    {
        Name = name;
        _schemaBuilder = schemaBuilder;
    }

    public TableBuilder AddColumn(string name, string type)
    {
        Columns.Add(new ColumnSymbol(name, type));
        return this;
    }

    public TableBuilder AddTable(string name)
        => _schemaBuilder.AddTable(name);

    public TableSymbol Build()
    {
        if (Columns.Any())
        {
            return new TableSymbol(Name, [.. Columns]);
        }

        return new TableSymbol(Name, [new ColumnSymbol(Guid.NewGuid().ToString("N"), string.Empty)]);
    }
}