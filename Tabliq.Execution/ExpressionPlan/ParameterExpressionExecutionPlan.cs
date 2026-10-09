using System.Reflection.Metadata;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ParameterExpressionExecutionPlan : ExpressionPlanNode
{
    public ParameterExpressionExecutionPlan(ParameterIdentifier parameter)
    {
        ParameterName = parameter.ParameterName;
        ParameterValue = parameter.Binding?.ParameterSymbol.State is ExecuterParameter executerParameter ? executerParameter.Value : null;
        Parameter = parameter.Binding?.ParameterSymbol;
    }

    public override string Identifier => $"Parameter({ParameterName})";

    public ParameterSymbol? Parameter { get; }

    public string ParameterName { get; }

    public object? ParameterValue { get; }

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
        => ParameterValue;

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
}
