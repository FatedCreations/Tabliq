using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class WhereClause : SyntaxNode
{
    public WhereClause(Condition condition)
    {
        Condition = condition;
    }

    public Condition Condition { get; }


    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is WhereClause where && Condition.Equals(where.Condition);
    }
}
