using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class CommonTableExpression : SyntaxNode
{
    public CommonTableExpression(string alias, SelectExpression selectExpression)
    {
        Alias = alias;
        Body = selectExpression;
    }
    public string Alias { get; }
    public SelectExpression Body { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Body;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is CommonTableExpression cte && Alias == cte.Alias && Body.Equals(cte.Body);
    }
}
