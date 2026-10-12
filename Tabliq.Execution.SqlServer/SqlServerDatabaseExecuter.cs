using Microsoft.Data.SqlClient;
using Tabliq.Execution.ExecutionReader;

namespace Tabliq.Execution.SqlServer;

public class SqlServerDatabaseExecuter : ISqlServerDatabaseExecuter
{
    public SqlServerDatabaseExecuter(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }


    public async Task<IExecutionReader> ExecuteAsync(string sql, IDictionary<string, object?>? paramaters = null, CancellationToken cancellationToken = default)
    {
        SqlConnection? con = null;
        SqlCommand? cmd = null;
        SqlDataReader? reader = null;
        try
        {
            con = new SqlConnection(ConnectionString);
            cmd = con.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = System.Data.CommandType.Text;

            if (paramaters is not null)
            {
                foreach (var param in paramaters)
                {
                    var sqlParam = cmd.CreateParameter();
                    sqlParam.ParameterName = param.Key;
                    sqlParam.Value = param.Value is null ? DBNull.Value : param.Value;
                    cmd.Parameters.Add(sqlParam);
                }
            }

            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            return new DbReaderExecutionReader(reader, con, cmd);
        }
        catch
        {
            if (reader is not null)
            {
                await reader.DisposeAsync();
            }
            if (cmd is not null)
            {
                await cmd.DisposeAsync();
            }
            if (con is not null)
            {
                await con.DisposeAsync();
            }

            throw;
        }
    }
}