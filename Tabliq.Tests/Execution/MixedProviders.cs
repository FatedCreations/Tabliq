using System.Text.RegularExpressions;
using Tabliq.Execution;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.Providers;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Printer;

namespace Tabliq.Tests.Execution;

public class MixedProviderExecutionTests
{
    private SimpleSqlServerProvider _sqlProvider;
    private IExecutionProvider _objectProvider;
    private ExecutionEngine _engine;

    public MixedProviderExecutionTests()
    {
        _sqlProvider = new SimpleSqlServerProvider();
        _sqlProvider.AddTable(new TableSymbol("Data", null,
        [
            new ColumnSymbol("Id", "int"),
            new ColumnSymbol("Name", "string"),
            new ColumnSymbol("NameTest", "string"),
            new ColumnSymbol("Value", "double"),
            new ColumnSymbol("Value2", "double"),
        ]));


        var test2 = ObjectProvider.Create(
            "Other",
            null,
            Enumerable.Range(0, 20).Select(x => new
            {
                Id = x,
                Name = $"Other{x}",
                NameOther = $"Other{x}",
                Value = 0.2 * x,
                Value2 = (0.2 * x) % 145,
            }).ToArray()
        );
        _objectProvider = test2;

        _engine = new ExecutionEngine([_sqlProvider, _objectProvider]);
    }

    [Fact]
    public async Task Join()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d LEFT JOIN Other o ON d.Id = o.Id
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        // this is falling down to the table scan for the Data table, and then the join is being done in memory with the Other table, which is an object provider
        Assert.Equal("""
            SELECT
                d.Id,
                d.Name
            FROM Data AS d
            """,
            _sqlProvider.LastSqlExecuted);

        // should this be optimised by adding in a select `id in ({in memory records})`
    }

    [Fact]
    public async Task MultiTable()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d, Other o
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        // this is falling down to the table scan for the Data table, and then the join is being done in memory with the Other table, which is an object provider
        Assert.Equal("""
            SELECT d.Name
            FROM Data AS d
            """,
            _sqlProvider.LastSqlExecuted);

        // should this be optimised by adding in a select `id in ({in memory records})`
    }

    [Fact]
    public async Task CountAggregateOnSqlProviderPassesThrough()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT COUNT(*) AS c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal("""
            SELECT COUNT(*) AS c
            FROM Data
            """,
            _sqlProvider.LastSqlExecuted);
    }

    [Fact]
    public async Task CountAggregateOnObjectProviderFallsBackToInMemory()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT COUNT(*) AS c FROM Other", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        var row = Assert.Single(results);
        Assert.Equal(20, (int)row["c"]!);
    }
}
