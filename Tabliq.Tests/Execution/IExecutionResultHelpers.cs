using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution;
using Tabliq.Execution.Providers;

namespace Tabliq.Tests.Execution;

public static class IExecutionResultHelpers
{
    public static async Task<List<Dictionary<string, object?>>> ToListAsync(this Task<IExecutionReader> executionReaderTask)
        => await ToListAsync(await executionReaderTask);

    public static async Task<List<Dictionary<string, object?>>> ToListAsync(this IExecutionReader executionReader)
    {
        var results = new List<Dictionary<string, object?>>();
        while (await executionReader.ReadAsync(default))
        {
            var row = new Dictionary<string, object?>();
            var values = executionReader.GetValues();
            var fields = executionReader.GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                row[fields[i]] = values[i];
            }
            results.Add(row);
        }
        return results;
    }

    public static async Task<List<Dictionary<string, object?>>> ExecuteToDictionaryList(this ExecutionEngine engine, string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken = default)
    {
        await using var results = await engine.ExecuteAsync(sql, parameters, cancellationToken);
        return await results.ToListAsync();
    }
    public static async Task<Results> BuildPlanAndExecuteToDictionaryList(this ExecutionEngine engine, string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken = default)
    {
        var plan = engine.BuildPlan(sql, parameters);
        await using var results = await plan.ExecuteAsync(parameters, cancellationToken);
        return new Results
        {
            Rows = await results.ToListAsync(),
            Plan = plan
        };
    }

    public class Results
    {
        public required List<Dictionary<string, object?>> Rows { get; init; }

        public required ExecutionPlanNode Plan { get; init; }
    }
}
