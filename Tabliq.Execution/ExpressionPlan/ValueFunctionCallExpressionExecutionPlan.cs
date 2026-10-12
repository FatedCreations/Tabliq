using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public sealed class ValueFunctionCallExpressionExecutionPlan : ExpressionPlanNode
{
    public ValueFunction Function { get; }
    public IReadOnlyList<ExpressionPlanNode> Arguments { get; }
    public FunctionCallExpression Expression { get; }  // todo remnove the need for this, we should be able to get the expression from the function call itself
    public override string Identifier => $"ValueFunctionCall({Function.Name}({string.Join(", ", Arguments.Select(a => a.Identifier))}))";
    public ValueFunctionCallExpressionExecutionPlan(FunctionCallExpression expression, ValueFunction function, IEnumerable<ExpressionPlanNode> arguments)
    {
        Function = function;
        Arguments = arguments.ToList();
        Expression = expression;
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => Arguments;

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        return Function.Execute(Expression, row, Arguments);
    }
}
