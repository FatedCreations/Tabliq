using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class CaseWhenClause : SyntaxNode
{
    public CaseWhenClause(Expression expression, Expression result)
    {
        Expression = expression;
        Result = result;
    }
    public Expression Expression{ get; }
    public Expression Result { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        yield return Result;
    }
    public override bool Equals(SyntaxNode? other)
    {
        if (other is not CaseWhenClause otherWhen)
            return false;
        return Expression.Equals(otherWhen.Expression) &&
               Result.Equals(otherWhen.Result);
    }
}