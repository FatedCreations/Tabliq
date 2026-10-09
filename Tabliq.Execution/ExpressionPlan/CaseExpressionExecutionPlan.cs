using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution.ExpressionPlan;

//public sealed class CaseExpressionExecutionPlan(CaseExpression caseExpression) : ExpressionPlanNode
//{
//    protected override object? Execute(RowAccessor row, IReadOnlyDictionary<FunctionCallExpression, object?>? aggregateValues = null)
//    {
//        if (caseExpression.Expression is not null)
//        {
//            var switchValue = Create(caseExpression.Expression).Execute(row, aggregateValues);
//            foreach (var when in caseExpression.WhenClauses)
//            {
//                if (when.Expression is Condition condition && ConditionExecutionPlan.Create(condition).Execute(row))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }

//                if (Equals(switchValue, Create(when.Expression).Execute(row, aggregateValues)))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }
//            }
//        }
//        else
//        {
//            foreach (var when in caseExpression.WhenClauses)
//            {
//                if (when.Expression is Condition condition && ConditionExecutionPlan.Create(condition).Execute(row))
//                {
//                    return Create(when.Result).Execute(row, aggregateValues);
//                }
//            }
//        }

//        return caseExpression.ElseResult is not null ? Create(caseExpression.ElseResult).Execute(row, aggregateValues) : null;
//    }
//}
