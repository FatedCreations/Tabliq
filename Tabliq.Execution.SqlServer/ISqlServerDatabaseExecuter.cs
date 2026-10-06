namespace Tabliq.Execution.SqlServer;

public interface ISqlServerDatabaseExecuter
{
    Task<IExecutionReader> ExecuteAsync(string sqlScript, IDictionary<string, object?>? paramaters, CancellationToken cancellationToken);
}
