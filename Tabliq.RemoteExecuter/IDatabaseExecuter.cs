using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter;

public interface IDatabaseExecuter
{
    public Task<ExecutionResult> ExecuteAsync(string sql, IDictionary<string, object> paramaters);
}

public class MsSqlDatabaseExecuter : IDatabaseExecuter
{
    public MsSqlDatabaseExecuter(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public Task<ExecutionResult> ExecuteAsync(string sql, IDictionary<string, object> paramaters)
    {
        throw new NotImplementedException();
    }
}