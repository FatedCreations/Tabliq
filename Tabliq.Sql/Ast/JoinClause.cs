using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class JoinClause : SyntaxNode
{
    public JoinClause( JoinSide joinSide, JoinType joinType, TableReference tableReference, Condition? onCondition)
    {
        JoinSide = joinSide;
        JoinType = joinType;
        TableReference = tableReference;
        OnCondition = onCondition;
    }

    public JoinSide JoinSide { get; }
    public JoinType JoinType { get; }
    public TableReference TableReference { get; }
    public Condition? OnCondition { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return TableReference;
        if (OnCondition is not null)
        {
            yield return OnCondition;
        }
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is JoinClause join &&
               JoinType == join.JoinType &&
               JoinSide == join.JoinSide &&
               TableReference.Equals(join.TableReference) &&
               ((OnCondition is null && join.OnCondition is null) || (OnCondition is not null && OnCondition.Equals(join.OnCondition)));
    }
}

public enum JoinSide
{
    Unspecified,
    Left,
    Full,
    Right,
}
public enum JoinType
{
    Unspecified,
    Inner,
    Outer,
    Cross
}
