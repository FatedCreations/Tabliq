using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class LogicalCondition : Condition
{
    public LogicalCondition(Condition left, LogicalOperator op, Condition right)
    {
        this.Left = left;
        this.Operator = op;
        this.Right = right;
    }

    public Condition Left { get; }
    public LogicalOperator Operator { get; }
    public Condition Right { get; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is LogicalCondition logicalCondition &&
               Left.Equals(logicalCondition.Left) &&
               Operator == logicalCondition.Operator &&
               Right.Equals(logicalCondition.Right);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }
}
public enum LogicalOperator
{
    And,
    Or
}