using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class IdentifierExpressionExecutionPlan : ExpressionPlanNode
{
    public IdentifierExpressionExecutionPlan(IdentifierExpression identifier)
    {
        TableSymbol = identifier.Binding?.TableSymbol;
        ColumnSymbol = identifier.Binding?.ColumnSymbol;

        var parts = identifier.GetColumnParts();
        ColumnName = ColumnSymbol?.Name ?? parts.ColumnName;
        TableName = parts.TableName ?? TableSymbol?.TableName;
    }

    public override string Identifier => $"Identifier({TableSymbol?.TableName ?? TableName}.{ColumnSymbol?.Name ?? ColumnName})";

    public TableSymbol? TableSymbol { get; }

    public ColumnSymbol? ColumnSymbol { get; }
    public string ColumnName { get; }
    public string? TableName { get; }

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var tableName = TableName;
        var column = ColumnName;
        return GetValue(row, tableName, column) ?? GetValue(row, null, ColumnName);
    }

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
