using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Printer;

namespace Tabliq.RemoteExecuter;

public interface IDatabaseExecuter
{
    public Task<ExecutionResult> ExecuteAsync(SqlScript sqlScript, IDictionary<string, object?>? paramaters, CancellationToken cancellationToken);
}
