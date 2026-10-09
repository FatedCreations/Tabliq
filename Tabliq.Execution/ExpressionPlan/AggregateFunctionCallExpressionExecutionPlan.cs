using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class AggregateFunctionCallExpressionExecutionPlan : ExpressionPlanNode
{
    private AggregateFunction Function { get; }
    public IReadOnlyList<ExpressionPlanNode> Arguments { get; }
    public FunctionCallExpression Expression { get; }  // todo remnove the need for this, we should be able to get the expression from the function call itself

    public override string Identifier => $"AggregateFunctionCall({Function.Name}({string.Join(", ", Arguments.Select(a => a.Identifier))}))";

    public AggregateFunctionCallExpressionExecutionPlan(FunctionCallExpression expression, AggregateFunction function, IEnumerable<ExpressionPlanNode> arguments)
    {
        Function = function;
        Arguments = arguments.ToList();
        Expression = expression;
    }

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        // the agg values are in the same set of rows but prefixed to distinguish.
        if (aggregateValues is not null && aggregateValues.TryGetValue(Expression, out var aggregateValue))
        {
            return aggregateValue;
        }

        return null;
    }

    internal AggregateFunctionState InitState()
        => Function.InitState();

    internal AggregateFunctionState ProcessRow( RowAccessor accessor, AggregateFunctionState state)
        => Function.ProcessRow(Expression, accessor, Arguments, state);

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => Arguments;
}
