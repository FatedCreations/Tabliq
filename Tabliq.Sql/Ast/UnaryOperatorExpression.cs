using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class UnaryOperatorExpression : Expression
{
    public UnaryOperatorExpression(Expression expression, UnaryOperator @operator)
    {
        Expression = expression;
        Operator = @operator;
    }

    public Expression Expression { get; }
    public UnaryOperator Operator { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    override public bool Equals(SyntaxNode? other)
    {
        if (other is not UnaryOperatorExpression otherUnary)
            return false;
        return Expression.Equals(otherUnary.Expression) && Operator == otherUnary.Operator;
    }
}

public enum UnaryOperator
{
    Unknown,
    Negate,
}