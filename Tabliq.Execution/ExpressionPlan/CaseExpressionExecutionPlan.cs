using System.Linq.Expressions;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;


public sealed class CaseExpressionEntryPlan
{
    public required ConditionExecutionPlan Condition { get; init; }

    public required ExpressionPlanNode Result { get; init; }
}

public sealed class CaseExpressionExecutionPlan : ExpressionPlanNode
{
    public CaseExpressionExecutionPlan(IEnumerable<CaseExpressionEntryPlan> caseExpressions, ExpressionPlanNode? elseValue)
    {
        Cases = caseExpressions.ToList();
        ElseValue = elseValue;
    }

    public IEnumerable<CaseExpressionEntryPlan> Cases { get; }

    public ExpressionPlanNode? ElseValue { get; }

    public override string Identifier => $"CaseExpression({string.Join(", ", Cases.Select(c => $"WHEN {c.Condition.Identifier} THEN {c.Result.Identifier}"))}{(ElseValue is not null ? $" ELSE {ElseValue.Identifier}" : "")})";

    public override IEnumerable<ExpressionPlanNode> GetExpressions()
        => Cases.SelectMany(c => c.Condition.GetExpressions()).Concat(Cases.Select(c => c.Result)).Append(ElseValue).Where(e => e is not null)!;

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        foreach (var caseExpression in Cases)
        {
            if (caseExpression.Condition.Execute(row))
            {
                return caseExpression.Result.ExecuteExpression(row, aggregateValues);
            }
        }
        if (ElseValue is not null)
        {
            return ElseValue.ExecuteExpression(row, aggregateValues);
        }
        return null;
    }
}
public sealed class SimpleCaseExpressionEntryPlan
{
    public required ExpressionPlanNode Target { get; init; }

    public required ExpressionPlanNode Result { get; init; }
}

public sealed class SimpleCaseExpressionExecutionPlan : ExpressionPlanNode
{
    public SimpleCaseExpressionExecutionPlan(ExpressionPlanNode value, IEnumerable<SimpleCaseExpressionEntryPlan> caseExpressions, ExpressionPlanNode? elseValue)
    {
        Value = value;
        Cases = caseExpressions.ToList();
        ElseValue = elseValue;
    }

    public ExpressionPlanNode Value { get; }
    public IEnumerable<SimpleCaseExpressionEntryPlan> Cases { get; }
    public ExpressionPlanNode? ElseValue { get; }

    public override string Identifier => $"SimpleCaseExpression({Value.Identifier}, {string.Join(", ", Cases.Select(c => $"WHEN {c.Target.Identifier} THEN {c.Result.Identifier}"))}{(ElseValue is not null ? $" ELSE {ElseValue.Identifier}" : "")})";

    public override IEnumerable<ExpressionPlanNode> GetExpressions()
        => Cases.Select(c => c.Target).Concat(Cases.Select(c => c.Result)).Append(ElseValue).Append(Value).Where(e => e is not null)!;

    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
    {
        var value = Value.ExecuteExpression(row, aggregateValues);

        foreach (var caseExpression in Cases)
        {
            var target = caseExpression.Target.ExecuteExpression(row, aggregateValues);

            if (ComparisonHelpers.AreEqual(value, target))
            {
                return caseExpression.Result.ExecuteExpression(row, aggregateValues);
            }
        }
        if (ElseValue is not null)
        {
            return ElseValue.ExecuteExpression(row, aggregateValues);
        }
        return null;
    }
}
