using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class IdentifierExpressionExecutionPlan : ExpressionPlanNode
{
    public IdentifierExpressionExecutionPlan(IdentifierExpression identifier)
    {
        ColumnBinding = identifier.Binding;
        var TableSymbol = identifier.Binding?.TableBinding.TableSymbol;
        var ColumnSymbol = identifier.Binding?.ColumnSymbol;

        var parts = identifier.GetColumnParts();
        ColumnName = ColumnSymbol?.Name ?? parts.ColumnName;
        TableName = identifier.Binding?.TableBinding.Alias ?? parts.TableName ?? TableSymbol?.TableName;
    }

    public override string Identifier => $"Identifier({ColumnBinding})";

    public ColumnBinding? ColumnBinding { get; }
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
