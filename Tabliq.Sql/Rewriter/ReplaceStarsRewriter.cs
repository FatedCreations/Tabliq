using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Parsing;

namespace Tabliq.Sql.Rewriter
{
    public class ReplaceStarsRewriter : SqlRewiter
    {
        public ReplaceStarsRewriter()
        {
        }

        protected virtual bool ShouldExpand(ColumnBinding binding)
        {
            // By default, expand all columns. Override this method to implement custom logic. hidden expansions etc
            return true;
        }
        protected virtual bool ShouldExpand(StarIdentifierExpression binding)
        {
            // By default, expand all columns. Override this method to implement custom logic. hidden expansions etc
            return true;
        }

        protected override SelectExpression Rewrite(SelectExpression node)
        {

            List<SelectProjection> projections = null!;
            var rewritten = false;
            if (node.Projections.Any(x => x.Expression is StarIdentifierExpression)) // can't be aliased, so we can just check the expression type
            {
                // rewite pending
                projections = new();
                foreach (var p in node.Projections)
                {
                    if (p.Expression is StarIdentifierExpression starIdentifier)
                    {

                        if (!ShouldExpand(starIdentifier))
                        {
                            projections.Add(p);
                            continue;
                        }

                        if (starIdentifier.Bindings is null || !starIdentifier.Bindings.Any())
                        {
                            Diagnostics.Report("BindingMissing", "StarIdentifierExpression has no bindings, cannot rewrite", starIdentifier);
                            projections.Add(p);
                        }
                        else
                        {
                            foreach (var b in starIdentifier.Bindings)
                            {
                                var tableSymbol = b.TableSymbol;
                                var columnSymbol = b.ColumnSymbol;
                                if (tableSymbol is null || columnSymbol is null)
                                {
                                    Diagnostics.Report("BindingMissing", "StarIdentifierExpression has a binding with missing table or column symbol, cannot rewrite", starIdentifier);
                                    continue;
                                }

                                // enable skipping some columns if you want to implement hidden columns or other logic
                                if (!ShouldExpand(b))
                                {
                                    continue;
                                }

                                // TODO: fetch addtional metadata about the column to see if we should include it in the projection list
                                var newProjection = new SelectProjection(
                                    new IdentifierExpression(tableSymbol.Name, columnSymbol.Name)
                                    {
                                        Binding = b,
                                    }.WithLocation(starIdentifier.Span),
                                    columnSymbol.Name,
                                    true
                                ).WithLocation(starIdentifier.Span);
                                rewritten = true;
                                projections.Add(newProjection);
                            }
                        }
                    }
                    else
                    {
                        projections.Add(p);
                    }
                }
            }

            if (rewritten)
            {
                node = new SelectExpression(
                       node.IsBracketed,
                       node.Top,
                       node.Distinctness,
                       projections,
                       node.From,
                       node.Where,
                       node.GroupBy,
                       node.Having,
                       node.OrderBy,
                       node.UnionStatements
                   );
            }

            // walk the remaining to process subqueries etc
            return base.Rewrite(node);
        }
    }
}
