using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution;
using Tabliq.Execution.Providers;

namespace Tabliq.Tests.Execution;

public class InMemoryExecutionTests
{
    private List<IExecutionProvider> _providers = [];
    private ExecutionEngine _engine;

    public InMemoryExecutionTests()
    {
        var test1 = ObjectProvider.Create(
            "Data",
            null,
            Enumerable.Range(0, 20).Select(x => new
            {
                Id = x,
                Name = $"Test{x}",
                NameTest = $"Test{x}",
                Value = 0.2 * x,
                Value2 = (0.2 * x) % 145,
            }).ToArray()
        );
        _providers.Add(test1);

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
        _providers.Add(test2);

        _engine = new ExecutionEngine(_providers);
    }

    [Fact]
    public async Task SelectStarFromSingleTable()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT * FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
    }
    [Fact]
    public async Task SelectProjection()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT Name as n FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
        var first = results.First();
        Assert.Single(first.Keys);
        Assert.Contains("n", first.Keys);
    }

    [Fact]
    public async Task CalculatedField()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT Value, Value + 1 as Calculated FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
        Assert.All(results, r => Assert.Equal((int)r["Value"] + 1, (int)r["Calculated"]));
    }


    [Fact]
    public async Task CountAggregrate()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT count(*) as c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        var row = Assert.Single(results);
        Assert.Equal(20, (int)row["c"]!);
    }

    [Fact]
    public async Task Join()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d LEFT JOIN Other o ON d.Id = o.Id
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
        var first = results.First();
        Assert.Equal(2, first.Keys.Count);
        Assert.Equal("Test0", first["n"]);
        Assert.Equal("Other0", first["oName"]);
    }

    [Fact]
    public async Task MultiTable()
    {
        var results = await _engine.ExecuteToDictionaryList("""
            SELECT d.Name as n, o.Name as oName FROM Data d, Other o
        """, Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20 * 20, results.Count);
    }
}
