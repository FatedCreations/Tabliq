using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class SelectExpression : Expression
{
    public SelectExpression(bool isBracketed, SelectExpression selectExpression)
        : this(
              isBracketed,
              selectExpression.Top,
              selectExpression.Distinctness,
              selectExpression.Projections,
              selectExpression.From,
              selectExpression.Where,
              selectExpression.GroupBy,
              selectExpression.Having,
              selectExpression.OrderBy,
              selectExpression.UnionStatements)
    {
        this.Span = selectExpression.Span;
    }

    public SelectExpression(
        bool isBracketed,
        long? top,
        Distinctness distinctness,
        IEnumerable<SelectProjection> projections,
        FromClause? from,
        WhereClause? where,
        GroupByClause? groupBy,
        HavingClause? having,
        OrderByClause? orderBy,
        IEnumerable<UnionStatement> unionStatements)
    {
        Distinctness = distinctness;
        Top = top;
        IsBracketed = isBracketed;
        Projections = new List<SelectProjection>(projections);
        From = from;
        Where = where;
        GroupBy = groupBy;
        Having = having;
        OrderBy = orderBy;
        UnionStatements = new List<UnionStatement>(unionStatements);
    }

    public bool IsBracketed { get; }
    public IReadOnlyList<SelectProjection> Projections { get; } = [];
    public FromClause? From { get; }
    public WhereClause? Where { get; }
    public GroupByClause? GroupBy { get; }
    public HavingClause? Having { get; }
    public OrderByClause? OrderBy { get; }
    public IReadOnlyList<UnionStatement> UnionStatements { get; } = [];
    public Distinctness Distinctness { get; }
    public long? Top { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var projection in Projections)
        {
            yield return projection;
        }
        if (From is not null)
        {
            yield return From;
        }

        if (Where is not null)
        {
            yield return Where;
        }
        if (GroupBy is not null)
        {
            yield return GroupBy;
        }

        if (Having is not null)
        {
            yield return Having;
        }

        if (OrderBy is not null)
        {
            yield return OrderBy;
        }

        foreach (var union in UnionStatements)
        {
            yield return union;
        }
    }

    public override bool Equals(SyntaxNode? other)
    {
        if (other is not SelectExpression select)
        {
            return false;
        }

        return
           IsBracketed == select.IsBracketed &&
           Top == select.Top &&
           Distinctness == select.Distinctness &&
           Projections.SyntaxSequenceEqual(select.Projections) &&
           ((From is null && select.From is null) || (From is not null && From.Equals(select.From))) &&
           ((Where is null && select.Where is null) || (Where is not null && Where.Equals(select.Where))) &&
           ((GroupBy is null && select.GroupBy is null) || (GroupBy is not null && GroupBy.Equals(select.GroupBy))) &&
           ((Having is null && select.Having is null) || (Having is not null && Having.Equals(select.Having))) &&
           ((OrderBy is null && select.OrderBy is null) || (OrderBy is not null && OrderBy.Equals(select.OrderBy))) &&
           UnionStatements.SyntaxSequenceEqual(select.UnionStatements);
    }
}