using Tabliq.Sql.Binding;

namespace Tabliq.Tests;

public class SchemaBuilder
{
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();
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

    public TableBuilder AddTable(TableSymbol symbol)
    {
        var b = this.AddTable(symbol.Name);
        foreach(var col in symbol.Columns)
        {
            b.Columns.Add(col);
        }

        return b;
    }
    public SchemaBuilder AddFunctions(IEnumerable<FunctionSymbol> functions)
    {
        foreach (var func in functions)
        {
            var b = AddFunction(func.Name);
            b.IsAggFunction(b.IsAgg);
            foreach (var a in func.Arguments)
            {
                b.AddArgument(a, false);
            }

            if (func.ParamsArgument is not null)
            {
                b.AddArgument(func.ParamsArgument, true);
            }
        }
        return this;
    }
    public FunctionBuilder AddFunction(string name)
    {
        var func = Functions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (func is null)
        {
            func = new FunctionBuilder(name, this);
            Functions.Add(func);

        }
        return func;
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
            [.. Parameters],
            Functions.Select(x => x.Build()).ToList());
    }

    public class SimpleSchema : ISchemaProvider
    {
        private readonly List<TableSymbol> _tables;
        private readonly List<ParameterSymbol> _parameters;
        private readonly List<FunctionSymbol> _functions;

        public SimpleSchema(List<TableSymbol> tables, List<ParameterSymbol> parameters, List<FunctionSymbol> functions)
        {
            _tables = tables;
            _parameters = parameters;
            _functions = functions;
        }

        public FunctionSymbol? GetFunction(string name)
            => _functions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ParameterSymbol? GetParameter(string name)
            => _parameters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public TableSymbol? GetTable(string name)
            => _tables.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

public class BuilderBase
{
    private readonly SchemaBuilder _schemaBuilder;
    public BuilderBase(SchemaBuilder schemaBuilder)
    {
        _schemaBuilder = schemaBuilder;
    }
    public TableBuilder AddTable(string name)
        => _schemaBuilder.AddTable(name);

    public FunctionBuilder AddFunction(string name)
        => _schemaBuilder.AddFunction(name);

}
public class FunctionBuilder : BuilderBase
{
    public List<FunctionArgumentSymbol> Arguments { get; } = new List<FunctionArgumentSymbol>();
    public FunctionArgumentSymbol? ParamsArgument { get; private set; }
    public bool IsAgg { get; private set; } = false;

    public string Name { get; }

    public FunctionBuilder(string name, SchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        Name = name;
    }

    public FunctionBuilder IsAggFunction(bool isAgg = true)
    {
        IsAgg = isAgg;
        return this;
    }

    public FunctionBuilder AddArgument(FunctionArgumentSymbol argument, bool paramsArgument = false)
    {
        if (paramsArgument)
        {
            ParamsArgument = argument;
        }
        else
        {
            Arguments.Add(argument);
        }
        return this;
    }
    public FunctionBuilder AddArgument(string Name, Type? RequiredType = null, BinderHandling BinderHandling = BinderHandling.Bind, bool Optional = false, bool paramsArgument = false)
    {
        var argument = new FunctionArgumentSymbol(Name, RequiredType, BinderHandling, Optional);
        return this.AddArgument(argument, paramsArgument);
    }

    public FunctionSymbol Build()
    {
        var args = Arguments.ToList();
        return new FunctionSymbol(Name, IsAggregate: IsAgg, args, ParamsArgument: ParamsArgument);
    }
}

public class TableBuilder : BuilderBase
{
    public List<ColumnSymbol> Columns { get; } = new List<ColumnSymbol>();

    public string Name { get; }

    public TableBuilder(string name, SchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        Name = name;
    }

    public TableBuilder AddColumn(string name, string type)
    {
        Columns.Add(new ColumnSymbol(name, type));
        return this;
    }

    public TableSymbol Build()
    {
        if (Columns.Any())
        {
            return new TableSymbol(Name, [.. Columns]);
        }

        return new TableSymbol(Name, [new ColumnSymbol(Guid.NewGuid().ToString("N"), string.Empty)]);
    }
}