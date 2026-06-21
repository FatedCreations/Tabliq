using System.Reflection;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Rewriter;

namespace Tabliq.RemoteExecuter;

internal class RemoteSqlRewiter : ReplaceStarsRewriter
{
    public static readonly RemoteSqlRewiter Instance = new RemoteSqlRewiter();


    protected override IdentifierExpression Rewrite(IdentifierExpression node)
    {
        if (node.Binding is not null)
        {
            if (node.Binding.ColumnSymbol.State is VirtualColumn vt)
            {
                node = new IdentifierExpression(node.Binding.TableSymbol.Name, vt.RemoteColumnName);
            }
        }

        return base.Rewrite(node);
    }
    protected override SelectProjection Rewrite(SelectProjection node)
    {
        // at this level we need to fixup missing alias prefix for virtual columns, because they are not bound to a table reference in the select projection
        if (node.Expression is IdentifierExpression idExpr)
        {
            if (idExpr.Binding?.ColumnSymbol.State is VirtualColumn vt) //will wi rewite this one!
            {
                if (node.Alias is null || node.IsSynthetic)
                {
                    // force syntetic so execution col names don't change!
                    node = new SelectProjection(idExpr, node.Alias ?? idExpr.Column, false);
                }
            }
        }

        return base.Rewrite(node);
    }

    private TableReference TryRewrite(TableReference node, ref bool hasChanges)
    {
        if (node is NamedTableReference namedTable)
        {
            if (namedTable.Binding is not null)
            {
                if (namedTable.Binding.State is VirtualTable vt)
                {
                    if (vt.RemoteSqlTableName is not null)
                    {
                        hasChanges = true;
                        return new NamedTableReference(vt.RemoteSqlTableName, namedTable.Alias ?? namedTable.Identifer.Column)
                        {
                            Binding = namedTable.Binding,
                            Span = namedTable.Span
                        };
                    }
                    else if (vt.RemoteSql is not null)
                    {
                        hasChanges = true;
                        return new SelectTableReference(vt.RemoteSql, namedTable.Alias ?? namedTable.Identifer.Column)
                        {
                            Span = namedTable.Span
                        };
                    }
                }
            }
        }

        return node;
    }

    protected override JoinClause Rewrite(JoinClause node)
    {
        var hasChanges = false;

        var tbl = TryRewrite(node.TableReference, ref hasChanges);
        var on = TryRewrite(node.OnCondition, ref hasChanges);

        if (hasChanges)
        {
            return new JoinClause(node.JoinSide, node.JoinType, tbl, on)
            {
                Span = node.Span,
            };
        }

        return base.Rewrite(node);
    }

    protected override FromClause Rewrite(FromClause node)
    {
        var tableRefs = new List<TableReference>();
        var hasChanges = false;
        foreach (var table in node.TableReferences)
        {
            tableRefs.Add(TryRewrite(table, ref hasChanges));
        }
        var joins = TryRewrite(node.Joins, ref hasChanges);

        if (hasChanges)
        {
            return new FromClause(tableRefs, joins);
        }

        return base.Rewrite(node);
    }
}
