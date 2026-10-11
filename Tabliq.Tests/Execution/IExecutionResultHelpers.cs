using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Execution;
using Tabliq.Execution.Providers;

namespace Tabliq.Tests.Execution;

public static class IExecutionResultHelpers
{
    public static async Task<List<RowData>> ToListAsync(this Task<IExecutionReader> executionReaderTask)
        => await ToListAsync(await executionReaderTask);

    public static async Task<List<RowData>> ToListAsync(this IExecutionReader executionReader)
    {
        var results = new List<RowData>();
        while (await executionReader.ReadAsync(default))
        {
            var values = executionReader.GetValues();
            var fields = executionReader.GetFields();

            results.Add(new RowData(fields.ToArray(), values.ToArray()));
        }
        return results;
    }

    public static async Task<List<RowData>> ExecuteToList(this ExecutionEngine engine, string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken = default)
    {
        await using var results = await engine.ExecuteAsync(sql, parameters, cancellationToken);
        return await results.ToListAsync();
    }
    public static async Task<Results> BuildPlanAndExecuteToList(this ExecutionEngine engine, string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken = default)
    {
        var plan = engine.BuildPlan(sql, parameters);
        await using var results = await plan.ExecuteAsync(parameters, cancellationToken);
        return new Results
        {
            Rows = await results.ToListAsync(),
            Plan = plan.RootNode,
            Diagnostics = plan.Diagnostics,
        };
    }

    public class Results
    {
        public required List<RowData> Rows { get; init; }

        public required ExecutionPlanNode Plan { get; init; }

        public required IReadOnlyList<ExecutionRewriteDiagnostic> Diagnostics { get; init; }
    }
    public struct RowData
    {
        private readonly string?[] _columns;

        private readonly object?[] _row;

        public RowData(string?[] columns, object?[] row)
        {
            _columns = columns;
            _row = row;
        }

        public string?[] Columns => _columns;

        public object?[] Values => _row;

        public object? this[string columnName]
        {
            get
            {
                if (TryGetValue(columnName, out var val))
                {
                    return val;
                }
                return null;
            }
        }
        public object? this[int columnIndex]
        {
            get
            {
                if (TryGetValue(columnIndex, out var val))
                {
                    return val;
                }
                return null;
            }
        }

        public bool TryGetValue(string columnName, out object? value)
        {
            if (_columns is null || _row is null)
            {
                value = null;
                return false;
            }
            for (var i = 0; i < _columns.Length; i++)
            {
                if (string.Equals(_columns[i], columnName, StringComparison.OrdinalIgnoreCase))
                {
                    value = _row[i];
                    return true;
                }
            }
            value = null;
            return false;
        }
        public bool TryGetValue(int columnIndex, out object? value)
        {
            if (_columns is null || _row is null)
            {
                value = null;
                return false;
            }
            if (columnIndex >= 0 && columnIndex < _row.Length)
            {
                value = _row[columnIndex];
                return true;
            }
            value = null;
            return false;
        }
    }
}
