using System.Diagnostics.CodeAnalysis;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public interface IExecutionProvider
{
    string Name { get; }

    IEnumerable<TableSymbol> GetTables();

    TableSymbol? GetTable(string tableName, string? schemaName = null);

    FunctionSymbol? GetFunction(string functionName);

    SqlScript? TryRewriteNode(SqlScript node);
}

public class ExecutionPlan
{
 
    public void BuildPlan(string sql, IEnumerable<ExecuterParameter> parameters, IEnumerable<IExecutionProvider> providers)
    {

        // Implementation for building the execution plan based on the provided script and execution provider.
    }
}


public class ExecuterParameter
{
    public ExecuterParameter()
    {
    }

    [SetsRequiredMembers]
    public ExecuterParameter(string name, object? value)
    {
        Name = name;
        Value = value;
    }

    public required string Name { get; set; }

    public required object? Value { get; set; }

    internal ParameterSymbol AsSymbol()
        => new ParameterSymbol(Name, "")
        {
            State = this
        };
}


internal sealed class ExecutionSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ParameterSymbol> _parameters = new Dictionary<string, ParameterSymbol>(StringComparer.OrdinalIgnoreCase);
    private readonly IEnumerable<IExecutionProvider> _providers;
    //private readonly InformationSchemaProvider _infoSchema;

    public ExecutionSchemaProvider(IEnumerable<IExecutionProvider> providers, IEnumerable<ExecuterParameter> parameters)
    {
        //_infoSchema = new InformationSchemaProvider(providers);
        // _providers = [.. providers, _infoSchema];
        _providers = providers;

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
    {
        var match = _providers.Select(x => (Provider: x, Function: x.GetFunction(name))).Where(x => x.Function is not null);

        if (match.Any())
        {
            var first = match.First().Function!;

            return new FunctionSymbol(first.Name, first.IsAggregate, first.Arguments, first.ParamsArgument)
            {
                State = new ExecutionProviderFunctionMetadata(match.ToDictionary(x => x.Provider, x => x.Function!)),
            };
        }
        return null;
    }

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
            return new TableSymbol(match.Table.TableName, match.Table.SchemaName, match.Table.Columns)
            {
                State = new ExecutionProviderTableMetadata(match.Table, match.Provider)
            };
        }

        return null;
    }
}

public sealed record ExecutionProviderTableMetadata(TableSymbol TableSymbol, IExecutionProvider ExecutionProvider);
public sealed record ExecutionProviderFunctionMetadata(IDictionary<IExecutionProvider, FunctionSymbol> Functions);