using Tabliq.Sql.Binding;
using Tabliq.Sql.Parsing;

namespace Tabliq.Execution;

public class ExecutionEngine
{
    private readonly IEnumerable<IExecutionProvider> _providers;

    public ExecutionEngine(IEnumerable<IExecutionProvider> providers)
    {
        _providers = providers;
    }

    public Task<IExecutionReader> ExecuteAsync(string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken)
    {
        parameters ??= Enumerable.Empty<ExecuterParameter>();

        var schema = new ExecutionSchemaProvider(_providers, parameters);

        var sqlResults = Parser.Parse(sql);
        sqlResults.ThrowIfInvalid();

        sqlResults = Binder.Bind(sqlResults, schema);
        sqlResults.ThrowIfInvalid();

        // now we can execute the sqlResults using the providers

        // we should generatea plan and execute it.
        // we nneed to build a full execute plan



        throw new NotImplementedException();
    }
}
