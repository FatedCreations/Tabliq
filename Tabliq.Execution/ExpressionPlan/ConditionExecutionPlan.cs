using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

public abstract class ConditionExecutionPlan
{
    public abstract bool Execute(RowAccessor row);

    public virtual ConditionExecutionPlan TryRewrite(ExecutionRewriteContext? context = null)
        => this;
    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();


    public static ConditionExecutionPlan Create(Condition expression)
        => expression switch
        {
            BinaryComparisonCondition comparison => new BinaryComparisonConditionExecutionPlan(ExpressionPlanNode.Create(comparison.Left), comparison.Operator, ExpressionPlanNode.Create(comparison.Right)),
            LogicalCondition logical => new LogicalConditionExecutionPlan(Create(logical.Left), logical.Operator, Create(logical.Right)),
            BracketedCondition bracketed => new BracketedConditionExecutionPlan(Create(bracketed.Expression)),
            UnaryCondition unary => new UnaryConditionExecutionPlan(Create(unary.Right), unary.Operator),
            IsNullCondition isNull => new IsNullConditionExecutionPlan(ExpressionPlanNode.Create(isNull.Expression), isNull.IsNot),
            LikeCondition like => new LikeConditionExecutionPlan(ExpressionPlanNode.Create(like.Left), ExpressionPlanNode.Create(like.Right), like.IsNot),
            BetweenCondition between => new BetweenConditionExecutionPlan(ExpressionPlanNode.Create(between.Left), ExpressionPlanNode.Create(between.From), ExpressionPlanNode.Create(between.To), between.IsNot),
            InListCondition inList => new InListConditionExecutionPlan(ExpressionPlanNode.Create(inList.Left), inList.Items.Select(ExpressionPlanNode.Create), inList.IsNot),
            InSelectCondition inSelect => new InSelectConditionExecutionPlan(ExecutionPlanNode.Create(inSelect.Expression)),
            ExistsCondition exists => new ExistsConditionExecutionPlan(ExecutionPlanNode.Create(exists.SelectExpression)),
            _ => throw new NotSupportedException($"Unsupported condition type: {expression.GetType().Name}")
        };

}
