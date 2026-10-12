using Tabliq.Execution.Functions;
using Tabliq.Sql.Binding;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tabliq.Execution;

internal sealed class ExecutionSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ParameterSymbol> _parameters = new Dictionary<string, ParameterSymbol>(StringComparer.OrdinalIgnoreCase);
    private readonly IEnumerable<IExecutionProvider> _providers;

    // todo: add information schema provider to the list of providers, so that we can get the information schema tables and functions from the providers
    //private readonly InformationSchemaProvider _infoSchema;
    private readonly IEnumerable<SqlFunction> _functions;

    public ExecutionSchemaProvider(IEnumerable<IExecutionProvider> providers, IEnumerable<ExecuterParameter> parameters, IEnumerable<SqlFunction> functions)
    {
        //_infoSchema = new InformationSchemaProvider(providers);
        // _providers = [.. providers, _infoSchema];
        _providers = providers;
        _functions = functions;

        // Convert the parameters dictionary to a list of ParameterSymbol objects
        // projected the data types from the c# types of the values in the dictionary
        foreach (var parameter in parameters)
        {
            var key = parameter.Name;
            var s = key.AsSpan();
            var finalKey = key;
            if (s.Length > 0)
            {
                if (s[0] == '@' || s[0] == ':')
                {
                    s = s.Slice(1);
                    finalKey = new string(s);
                }
                _parameters[finalKey] = new ParameterSymbol(finalKey, string.Empty)
                {
                    State = parameter.Value
                };
            }
        }
    }

    public FunctionSymbol? GetFunction(string name)
        => _functions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.FunctionSymbol;

    public ParameterSymbol? GetParameter(string name)
    {
        if (_parameters.TryGetValue(name, out var parameter))
        {
            return parameter;
        }
        return null;
    }

    public TableSymbol? GetTable(string name, string? schemaName = null)
    {
        var match = _providers.Select(x => (Provider: x, Table: x.GetTable(name, schemaName))).FirstOrDefault(x => x.Table is not null);

        if (match.Table is not null)
        {
            return match.Table.WithState(new ExecutionProviderTableMetadata(match.Table, match.Provider));
        }

        return null;
    }
}

internal sealed record ExecutionProviderTableMetadata(TableSymbol TableSymbol, IExecutionProvider ExecutionProvider);
