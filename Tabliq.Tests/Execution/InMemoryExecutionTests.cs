using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution;
using Tabliq.Execution.Functions;
using Tabliq.Execution.Providers;
using Tabliq.Sql.Ast;

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
                NameTest = $"Test{x % 5}",
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


        _engine = new ExecutionEngine(_providers, [new CustomAggregateFunction(), new CustomValueFunction()]);
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
    public async Task StarProjectionColumnsAreResolvedBeforeStreaming()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT *, Value + 1 as Calculated FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
        var first = results.First();
        Assert.Equal(6, first.Keys.Count);
        Assert.Equal("Test0", first["Name"]);
        Assert.Equal(1, first["Calculated"]);
    }

    [Fact]
    public async Task CountAggregrate()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT count(*) as c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        var row = Assert.Single(results);
        Assert.Equal(20, (int)row["c"]!);
    }

    [Fact]
    public async Task CustomAggregrate()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT CUST_AGG(Id) as c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        var row = Assert.Single(results);
        Assert.Equal(CustomAggregateFunction.TestValue, row["c"]!);
    }

    [Fact]
    public async Task CustomAggregrateWithGroupBy()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT CUST_AGG(Id) as c FROM Data group by NameTest", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(5, results.Count);
        Assert.All(results, row => Assert.Equal(CustomAggregateFunction.TestValue, row["c"]!));
    }

    [Fact]
    public async Task CustomValue()
    {
        var results = await _engine.ExecuteToDictionaryList("SELECT CUST_VALUE(Id) as c FROM Data", Enumerable.Empty<ExecuterParameter>(), CancellationToken.None);

        Assert.Equal(20, results.Count);
        Assert.All(results, row => Assert.Equal(CustomValueFunction.TestValue, row["c"]!));
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

    private class CustomAggregateFunction : AggregateFunction
    {
        public static object TestValue { get; } = new object();

        public CustomAggregateFunction()
            : base("CUST_AGG", [new FunctionArgument("value")])
        {
        }

        // we be called multiple times once for each row, but we just return the same value for testing purposes
        public override AggregateFunctionState ProcessRow(FunctionCallExpression expression, RowAccessor accessor, AggregateFunctionState state)
            => AggregateFunctionState.FromResult(TestValue);

        public override AggregateFunctionState InitState()
            => AggregateFunctionState.NullState;
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
