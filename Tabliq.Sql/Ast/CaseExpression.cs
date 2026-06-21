using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class CaseExpression : Expression
{
    public CaseExpression(Expression? expression, IEnumerable<CaseWhenClause> whenClauses, Expression? elseExpression)
    {
        Expression = expression;
        WhenClauses = [.. whenClauses];
        ElseResult = elseExpression;
    }
    public Expression? Expression { get; }
    public IReadOnlyList<CaseWhenClause> WhenClauses { get; }
    public Expression? ElseResult { get; }
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Expression != null)
            yield return Expression;
        foreach (var whenClause in WhenClauses)
            yield return whenClause;
        if (ElseResult != null)
            yield return ElseResult;
    }
    public override bool Equals(SyntaxNode? other)
    {
        if (other is not CaseExpression otherCase)
            return false;
        return Equals(Expression, otherCase.Expression) &&
               WhenClauses.SequenceEqual(otherCase.WhenClauses) &&
               Equals(ElseResult, otherCase.ElseResult);
    }
}
