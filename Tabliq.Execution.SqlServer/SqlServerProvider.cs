using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution.Functions;
using Tabliq.Execution.Providers;
using Tabliq.Execution.SqlServer.Internal;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.SqlServer;

public class SqlServerProvider : RemoteSqlProviderBase
{
    private readonly ISqlServerDatabaseExecuter _databaseExecuter;
    private readonly VirtualSchema _schema;
    private readonly IEnumerable<TableSymbol> _tables;

    public SqlServerProvider(VirtualSchema schema, string connectionString)
        : this(schema, new SqlServerDatabaseExecuter(connectionString))
    {
    }

    public SqlServerProvider(VirtualSchema schema, ISqlServerDatabaseExecuter databaseExecuter)
    {
        _databaseExecuter = databaseExecuter;
        _schema = schema;

        _tables = schema.Tables.Select(x => x.AsSymbol());
    }

    public override IEnumerable<TableSymbol> GetTables() => _tables;

    public static string GenerateSqlQuery(SqlScript script)
    {
        var parsed = RemoteSqlRewiter.Instance.Execute(script);
        parsed.ThrowIfInvalid();

        parsed = RewriteForMsSqlServer.Instance.Execute(parsed);
        parsed.ThrowIfInvalid();

        var sql = new MsSqlServerWriter().ToSql(parsed.Script);
        return sql;
    }

    private ReadOnlySpan<string> SupportedFunctions => new string[] {
        "COUNT",
        "RIGHT",
        "LEFT",
        "YEAR",
        "MONTH",
        "DAY"
    };

    protected override bool SupportsFunction(FunctionSymbol function)
        => SupportedFunctions.Contains(function.Name, StringComparer.OrdinalIgnoreCase);

    public override Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => _databaseExecuter.ExecuteAsync(GenerateSqlQuery(new SqlScript([sqlScript])), parameters?.ToDictionary(p => p.Name, p => p.Value), cancellationToken);
}
