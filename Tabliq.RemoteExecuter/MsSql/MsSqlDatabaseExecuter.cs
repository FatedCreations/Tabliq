using Tabliq.Sql.Ast;
using Tabliq.Sql.Parsing;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter.MsSql;

public class MsSqlDatabaseExecuter : IDatabaseExecuter
{
    public MsSqlDatabaseExecuter(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public async Task<ExecutionResult> ExecuteAsync(SqlScript sqlScript, IDictionary<string, object?>? paramaters, CancellationToken cancellationToken)
    {
        var parsed = RewriteForMsSqlServer.Instance.Execute(sqlScript);

        parsed.ThrowIfInvalid();

        var sql = SqlWriter.ToSql(parsed.Script);

        await using var con = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await using var cmd = con.CreateCommand();

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

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var result = new ExecutionResult();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result.Columns.Add(reader.GetName(i));
        }

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new object?[reader.FieldCount];
            reader.GetValues(row);
            for (var i = 0; i < row.Length; i++)
            {
                if (row[i] is DBNull)
                {
                    row[i] = null;
                }
            }

            result.Data.Add(row);
        }

        return result;
    }
}