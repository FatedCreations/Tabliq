using System.Text.RegularExpressions;
using Tabliq.Execution;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.Functions;
using Tabliq.Execution.Providers;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Printer;
using static Tabliq.Execution.Functions.SqlFunction;

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
        var results = await _engine.ExecuteToDictionaryList("SELECT CUST_VALUE(NameTest) AS c FROM Data WHERE NameTest = 'Test' GROUP BY NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT NameTest
            FROM Data
            WHERE NameTest = 'Test'
            GROUP BY NameTest
            """,
            _provider.LastSqlExecuted);
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
            return TestValue;
        }
    }
}


public class SimpleSqlServerProvider : RemoteSqlProviderBase
{
    public List<string> SqlExecuted { get; private set; } = new List<string>();

    public string? LastSqlExecuted => SqlExecuted.LastOrDefault();

    public override Task<IExecutionReader> ExecuteAsync(SelectExpression sqlScript, CancellationToken cancellationToken)
    {
        SqlExecuted.Add(sqlScript.ToString());
        return Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(Array.Empty<string>(), Enumerable.Empty<object?[]>().GetEnumerator(), Array.Empty<IAsyncDisposable>()));
    }
}