using Tabliq.Execution;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.Functions;
using Tabliq.Execution.Providers;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using static Tabliq.Execution.Providers.RemoteSqlProviderBase;

namespace Tabliq.Tests.Execution;

public class SqlServerExecutionTests
{
    private SimpleSqlServerProvider _provider;
    private ExecutionEngine _engine;

    public SqlServerExecutionTests()
    {
        _provider = new SimpleSqlServerProvider();
        _provider.AddTable(new TableSymbol("Data", null,
        [
            new ColumnSymbol("Id", "int"),
            new ColumnSymbol("Name", "string"),
            new ColumnSymbol("NameTest", "string"),
            new ColumnSymbol("Value", "double"),
            new ColumnSymbol("Value2", "double"),
        ]));

        _provider.AddTable(new TableSymbol("Other", null,
        [
            new ColumnSymbol("Id", "int"),
            new ColumnSymbol("Name", "string"),
            new ColumnSymbol("NameOther", "string"),
            new ColumnSymbol("Value", "double"),
            new ColumnSymbol("Value2", "double"),
        ]));
        _engine = new ExecutionEngine([_provider], [new CustomValueFunction()]);
    }

    [Fact]
    public async Task Cte()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            with d as (SELECT *
            FROM Data)
            SELECT * FROM d
            """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            with d as (
                SELECT *
                FROM Data
            )
            SELECT *
            FROM d
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task SelectStarFromSingleTable()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT * FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT *
            FROM Data
            """,
            _provider.LastSqlExecuted);
    }
    [Fact]
    public async Task SelectProjection()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT Name as n FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT Name AS n
            FROM Data
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task CalculatedField()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT Value, Value + 1 as Calculated FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT
                Value,
                Value + 1 AS Calculated
            FROM Data
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task Join()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d LEFT JOIN Other o ON d.Id = o.Id
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT
                d.Name AS n,
                o.Name AS oName
            FROM Data AS d
            LEFT JOIN Other AS o
                ON d.Id = o.Id
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task CountAggregatePassesThroughToSqlProvider()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT COUNT(*) AS c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT COUNT(*) AS c
            FROM Data
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task CountAggregatePassesThroughToSqlProviderWithGroupBy()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT COUNT(*) AS c FROM Data Group By NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT COUNT(*) AS c
            FROM Data
            GROUP BY NameTest
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public void RewrittenSqlServerFunctionsAreAllowedForPushdown()
    {
        Assert.True(_provider.IsSupported(new FunctionSymbol("CEIL", false, [new FunctionArgumentSymbol("value")] )));
        Assert.True(_provider.IsSupported(new FunctionSymbol("CHAR_LENGTH", false, [new FunctionArgumentSymbol("value")] )));
        Assert.True(_provider.IsSupported(new FunctionSymbol("EXTRACT", false, [new FunctionArgumentSymbol("part", BinderHandling: BinderHandling.Skip), new FunctionArgumentSymbol("value")] )));
        Assert.True(_provider.IsSupported(new FunctionSymbol("POSITION", false, [new FunctionArgumentSymbol("needle"), new FunctionArgumentSymbol("haystack")] )));
    }

    [Fact]
    public async Task UnrecognisedCustomValueTriggersAFilterdTableScan()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT CUST_VALUE(NameTest) AS c FROM Data WHERE NameTest = 'Test'", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT NameTest
            FROM Data
            WHERE NameTest = 'Test'
            """,
            _provider.LastSqlExecuted);
    }

    [Fact]
    public async Task UnrecognisedCustomValueTriggersAFilterdTableScanGroupByFirst()
    {
        _provider.Returns(s =>
            new[]{
                new
                {
                    NameTest = "Test"
                }
            });
        var results = await _engine.BuildPlanAndExecuteToDictionaryList("SELECT CUST_VALUE(NameTest) AS c FROM Data WHERE NameTest = 'Test' GROUP BY NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT NameTest
            FROM Data
            WHERE NameTest = 'Test'
            GROUP BY NameTest
            """,
            _provider.LastSqlExecuted);

        // should not be executing the sql directly, it should be wrapped in a ProjectionExecutionPlanNode that reprocesses the scan in memory
        Assert.IsNotType<RemoteSqProviderSqlExecutionPlanNode>(results.Plan);

        var row = Assert.Single(results.Rows);
        Assert.Equal("Test#CUST_VALUE", row["c"]);
    }


    [Fact]
    public async Task UnsupportedFunctionReportsPushdownDiagnostics()
    {
        _provider.Returns(s =>
            new[]
            {
                new
                {
                    NameTest = "Test"
                }
            });

        var results = await _engine.BuildPlanAndExecuteToDictionaryList("SELECT CUST_VALUE(NameTest) AS c FROM Data WHERE NameTest = 'Test' GROUP BY NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Contains(results.Diagnostics, x => x.Id == "SqlPushdownPartial");
        Assert.Contains(results.Diagnostics, x => x.Message.Contains("unsupported function", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SubSelect()
    {
        _provider.Returns(s =>
            new[]{
                new
                {
                    NameTest = "Test"
                }
            });
        var results = await _engine.BuildPlanAndExecuteToDictionaryList("SELECT Left(d.NameTest, 10) AS c FROM (SELECT NameTest FROM Data) as d WHERE NameTest = 'Test' GROUP BY NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.IsType<RemoteSqProviderSqlExecutionPlanNode>(results.Plan);

        Assert.Equal("""
            SELECT LEFT(d.NameTest, 10) AS c
            FROM (
                SELECT NameTest
                FROM Data
            ) AS d
            WHERE NameTest = 'Test'
            GROUP BY NameTest
            """,
            _provider.LastSqlExecuted);

        //var row = Assert.Single(results.Rows);
        //Assert.Equal("Test#CUST_VALUE", row["c"]);
    }

    [Fact]
    public async Task SubSelectWithCustomAgg()
    {
        _provider.Returns(s =>
            new[]{
                new
                {
                    NameTest = "Test"
                }
            });
        var results = await _engine.BuildPlanAndExecuteToDictionaryList("SELECT CUST_VALUE(d.NameTest) AS c FROM (SELECT NameTest FROM Data) as d WHERE NameTest = 'Test' GROUP BY NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT d.NameTest
            FROM (
                SELECT NameTest
                FROM Data
            ) AS d
            WHERE NameTest = 'Test'
            GROUP BY NameTest
            """,
            _provider.LastSqlExecuted);

        // should not be executing the sql directly, it should be wrapped in a ProjectionExecutionPlanNode that reprocesses the scan in memory
        Assert.IsNotType<RemoteSqProviderSqlExecutionPlanNode>(results.Plan);

        var row = Assert.Single(results.Rows);
        Assert.Equal("Test#CUST_VALUE", row["c"]);
    }


    [Fact]
    public async Task MultiTable()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d, Other o
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT
                d.Name AS n,
                o.Name AS oName
            FROM
                Data AS d,
                Other AS o
            """,
            _provider.LastSqlExecuted);
    }

    private class CustomValueFunction : ValueFunction
    {
        public static object TestValue { get; } = new object();

        public CustomValueFunction()
            : base("CUST_VALUE", [new FunctionArgument("value")])
        {
        }

        public override object? Execute(FunctionCallExpression expression, RowAccessor accessor)
        {
            var val = EvaluationHelpers.EvaluateExpression(expression.Arguments[0], accessor);
            return $"{val}#CUST_VALUE";
        }
    }
}


public class SimpleSqlServerProvider : RemoteSqlProviderBase
{
    private List<TableSymbol> _tables = new List<TableSymbol>();
    private List<Func<SelectStatement, IExecutionReader?>> _dataProviders = new();

    public void AddTable(TableSymbol tableSymbol)
    {
        _tables.Add(new TableSymbol(tableSymbol.TableName, tableSymbol.SchemaName, tableSymbol.Columns));

    }
    public void Returns<T>(Func<SelectStatement, IEnumerable<T>?> dataProvider)
    {
        _dataProviders.Add((s) =>
        {
            var data = dataProvider(s);

            if (data is null)
            {
                return null;
            }

            IEnumerable<object?[]> Rows()
            {
                var fields = typeof(T).GetProperties();
                object?[] row = new object?[fields.Length];

                foreach (var r in data)
                {
                    for (var i = 0; i < fields.Length; i++)
                    {
                        row[i] = fields[i].GetValue(r);
                    }

                    yield return row;
                }
            }

            var cols = s.SelectQuery.Projections.Select(x => x.Alias ?? x.Expression.ToString()).ToArray();

            return new EnumeratorExecutionReader(cols, Rows().GetEnumerator(), Array.Empty<IAsyncDisposable>());
        });
    }

    private ReadOnlySpan<string> SupportedFunctions => new string[] {
        "COUNT",
        "RIGHT",
        "LEFT",
        "YEAR",
        "MONTH",
        "DAY",
        "CEILING",
        "CHARINDEX",
        "DATEPART",
        "LEN"
    };

    public bool IsSupported(FunctionSymbol function)
        => IsFunctionSupportedForPushdown(new FunctionCallExpression(function.Name, [], null)
        {
            Binding = function,
        });

    protected override FunctionCallExpression RewriteFunctionCallForPushdown(FunctionCallExpression functionCall)
    {
        var rewrittenName = functionCall.FunctionName;
        if (rewrittenName.Equals("CEIL", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenName = "CEILING";
        }
        else if (rewrittenName.Equals("CHAR_LENGTH", StringComparison.OrdinalIgnoreCase)
            || rewrittenName.Equals("CHARACTER_LENGTH", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenName = "LEN";
        }
        else if (rewrittenName.Equals("EXTRACT", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenName = "DATEPART";
        }
        else if (rewrittenName.Equals("POSITION", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenName = "CHARINDEX";
        }
        else if (rewrittenName.Equals("LN", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenName = "LOG";
        }

        return rewrittenName == functionCall.FunctionName
            ? functionCall
            : new FunctionCallExpression(rewrittenName, functionCall.Arguments, functionCall.Window)
            {
                Span = functionCall.Span,
                Binding = functionCall.Binding,
            };
    }

    protected override bool SupportsFunction(FunctionSymbol function)
        => SupportedFunctions.Contains(function.Name, StringComparer.OrdinalIgnoreCase);

    public override IEnumerable<TableSymbol> GetTables() => _tables;

    public List<(SelectStatement Sql, IEnumerable<ExecuterParameter>? Parameters)> SqlExecuted { get; private set; } = [];

    public string LastSqlExecuted => SqlExecuted.LastOrDefault().Sql.ToString();
    public SelectStatement LastSqlStatementExecuted => SqlExecuted.LastOrDefault().Sql;
    public IEnumerable<ExecuterParameter>? LastParametersExecuted => SqlExecuted.LastOrDefault().Parameters;

    public override Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        SqlExecuted.Add((sqlScript, parameters));

        var reader = _dataProviders.Select(x => x.Invoke(sqlScript)).FirstOrDefault(x => x is not null);

        if (reader is null)
        {

            var cols = sqlScript.SelectQuery.Projections.Select(x => x.Alias ?? x.Expression.ToString()).ToArray();
            reader = new EmptyExecutionReader(cols);
        }

        return Task.FromResult<IExecutionReader>(reader);
    }
}