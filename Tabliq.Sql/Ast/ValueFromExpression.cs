using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class ValueFromExpression : Expression
{
    public string Part { get; }
    public Expression Expression { get; }

    public ValueFromExpression(string part, Expression expression)
    {
        Part = part;
        Expression = expression;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is ValueFromExpression ve && ve.Part.Equals(Part, StringComparison.OrdinalIgnoreCase) && Expression.Equals(ve.Expression);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }
}
