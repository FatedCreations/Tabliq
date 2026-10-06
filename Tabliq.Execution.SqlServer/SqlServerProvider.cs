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
        "DATE_BUCKET",
        "DATEADD",
        "DATEDIFF",
        "DATEDIFF_BIG",
        "DATEFROMPARTS",
        "DATENAME",
        "DATEPART",
        "DATETIME2FROMPARTS",
        "DATETIMEFROMPARTS",
        "DATETIMEOFFSETFROMPARTS",
        "SMALLDATETIMEFROMPARTS",
        "SWITCHOFFSET",
        "TIMEFROMPARTS",
        "TODATETIMEOFFSET",
        "DATETRUNC",
        "DAY",
        "EOMONTH",
        "GETDATE",
        "GETUTCDATE",
        "ISDATE",
        "MONTH",
        "SYSDATETIME",
        "SYSDATETIMEOFFSET",
        "SYSUTCDATETIME",
        "YEAR",
        "COUNT",
        "COUNT_BIG",
        "PRODUCT",
        "STDEV",
        "STDEVP",
        "VAR",
        "VARP",
        "CUME_DIST",
        "FIRST_VALUE",
        "LAG",
        "LAST_VALUE",
        "LEAD",
        "PERCENTILE_CONT",
        "PERCENTILE_DISC",
        "PERCENT_RANK",
        "ABS",
        "ACOS",
        "ASIN",
        "ATAN",
        "ATN2",
        "COS",
        "COT",
        "DEGREES",
        "EXP",
        "FLOOR",
        "CEILING",
        "LOG",
        "LOG10",
        "PI",
        "POWER",
        "RADIANS",
        "RAND",
        "ROUND",
        "SIGN",
        "SIN",
        "SQRT",
        "SQUARE",
        "TAN",
        "GREATEST",
        "LEAST",
        "IIF",
        "DENSE_RANK",
        "NTILE",
        "RANK",
        "ROW_NUMBER",
        "ASCII",
        "CHAR",
        "CHARINDEX",
        "CONCAT",
        "CONCAT_WS",
        "DIFFERENCE",
        "FORMAT",
        "LEFT",
        "LEN",
        "LOWER",
        "LTRIM",
        "NCHAR",
        "PATINDEX",
        "RIGHT",
        "RTRIM",
        "TRIM",
        "SUBSTRING",
        "CONVERT",
        "CAST",
        "PARSE",
        "TRY_CAST",
        "TRY_PARSE",
        "TRY_CONVERT",
        "AVG",
        "SUM",
        "ROLLUP",
        "CUBE"
    };

    protected override FunctionCallExpression RewriteFunctionCallForPushdown(FunctionCallExpression functionCall)
    {
        var script = new SqlScript([
            new SelectStatement([], new SelectExpression(
                false,
                null,
                Distinctness.Unspecified,
                [new SelectProjection(functionCall)],
                null,
                null,
                null,
                null,
                null,
                []))
        ]);

        var rewritten = RewriteForMsSqlServer.Instance.Execute(script);
        rewritten.ThrowIfInvalid();

        var projection = rewritten.Script.Statements
            .OfType<SelectStatement>()
            .SingleOrDefault()?
            .SelectQuery.Projections
            .SingleOrDefault();

        return projection?.Expression as FunctionCallExpression ?? functionCall;
    }

    protected override bool SupportsFunction(FunctionSymbol function)
        => SupportedFunctions.Contains(function.Name, StringComparer.OrdinalIgnoreCase);

    public override Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => _databaseExecuter.ExecuteAsync(GenerateSqlQuery(new SqlScript([sqlScript])), parameters?.ToDictionary(p => p.Name, p => p.Value), cancellationToken);
}
