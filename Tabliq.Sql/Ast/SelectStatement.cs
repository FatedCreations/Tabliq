using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

// this is the top level select statement, that can include ctes
public class SelectStatement : Statement
{
    public SelectStatement(IEnumerable<CommonTableExpression> commonTableExpressions, SelectExpression selectQuery)
    {
        CommonTableExpressions = new List<CommonTableExpression>(commonTableExpressions);
        SelectQuery = selectQuery;
    }

    public IReadOnlyList<CommonTableExpression> CommonTableExpressions { get; } = new List<CommonTableExpression>();

    public SelectExpression SelectQuery { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var cte in CommonTableExpressions)
        {
            yield return cte;
        }

        yield return SelectQuery;
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is SelectStatement selectStatement &&
               CommonTableExpressions.SyntaxSequenceEqual(selectStatement.CommonTableExpressions) &&
               SelectQuery.Equals(selectStatement.SelectQuery);
    }
}
