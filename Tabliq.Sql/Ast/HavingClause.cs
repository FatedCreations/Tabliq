using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class HavingClause : SyntaxNode
{
    public HavingClause(Condition condition)
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
        return other is HavingClause having && Condition.Equals(having.Condition);
    }
}
