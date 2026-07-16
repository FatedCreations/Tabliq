using System.Diagnostics.CodeAnalysis;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Parsing;
using Tabliq.Sql.Printer;

namespace Tabliq.Sql.Rewriter
{
    public abstract class SqlRewiter
    {
        protected DiagnosticBag Diagnostics => _diagnostics;
        private DiagnosticBag _diagnostics = null!;
        public CompilationResult Execute(CompilationResult syntaxTree)
        {
            _diagnostics = new DiagnosticBag();
            var diags = new List<Diagnostic>();

            diags.AddRange(syntaxTree.Diagnostics);
            diags.AddRange(_diagnostics.Diagnostics);

            var newTree = Rewrite(syntaxTree.Script);
            var script = (newTree as SqlScript) ?? new SqlScript([]);

            // Binding logic will go here
            return new CompilationResult(syntaxTree.Text, script, syntaxTree.Tokens, diags);
        }

        public CompilationResult Execute(SqlScript syntaxTree)
        {
            _diagnostics = new DiagnosticBag();
            var diags = new List<Diagnostic>();

            diags.AddRange(_diagnostics.Diagnostics);

            var newTree = Rewrite(syntaxTree);
            var script = (newTree as SqlScript) ?? new SqlScript([]);

            // Binding logic will go here
            return new CompilationResult(script, diags);
        }

        protected virtual SyntaxNode Rewrite(SyntaxNode node)
        {
            SyntaxNode resultSyntaxNode = node switch
            {
                BracketedExpression s => Rewrite(s),
                SqlScript s => Rewrite(s),
                SelectStatement s => Rewrite(s),
                SelectExpression s => Rewrite(s),
                SelectProjection s => Rewrite(s),
                IdentifierExpression s => Rewrite(s),
                LiteralExpression s => Rewrite(s),
                FromClause s => Rewrite(s),
                NamedTableReference s => Rewrite(s),
                UnionStatement s => Rewrite(s),
                OrderByClause s => Rewrite(s),
                OrderByEntry s => Rewrite(s),
                CaseExpression s => Rewrite(s),
                CaseWhenClause s => Rewrite(s),
                BinaryComparisonCondition s => Rewrite(s),
                FunctionCallExpression s => Rewrite(s),
                AsExpression s => Rewrite(s),
                StarIdentifierExpression s => Rewrite(s),
                GroupByClause s => Rewrite(s),
                BinaryOperatorExpression s => Rewrite(s),
                WhereClause s => Rewrite(s),
                ExistsCondition s => Rewrite(s),
                LogicalCondition s => Rewrite(s),
                CommonTableExpression s => Rewrite(s),
                IsNullCondition s => Rewrite(s),
                JoinClause s => Rewrite(s),
                UnaryCondition s => Rewrite(s),
                ValueFromExpression s => Rewrite(s),
                HavingClause s => Rewrite(s),
                SelectTableReference s => Rewrite(s),
                BracketedCondition s => Rewrite(s),
                ParameterIdentifier s => Rewrite(s),
                WindowSpecification s => Rewrite(s),
                DistinctValueExpression s => Rewrite(s),
                OffsetClause s => Rewrite(s),
                LikeCondition s => Rewrite(s),
                DataType s => Rewrite(s),
                BetweenCondition s => Rewrite(s),
                InSelectCondition s => Rewrite(s),
                InListCondition s => Rewrite(s),
                CurrentDate s => Rewrite(s),
                CurrentTimestamp s => Rewrite(s),
                CurrentTime s => Rewrite(s),
                NullValue s => Rewrite(s),
                EmptyStatement s => Rewrite(s),
                WithinGroupClause s => Rewrite(s),
                OverClause s => Rewrite(s),
                _ => throw new Exception($"Unhandled node type: {node?.GetType().Name}")
            };
            resultSyntaxNode.Span = node.Span;
            return resultSyntaxNode;
        }

        protected virtual OverClause Rewrite(OverClause node)
        {
            var rewritten = false;
            var Partions = TryRewrite(node.Partions, ref rewritten);
            var OrderBy = TryRewrite(node.OrderBy, ref rewritten);
            if (!rewritten)
            {
                return node;
            }

            return new OverClause(Partions, OrderBy).WithLocation(node.Span);
        }

        protected virtual WithinGroupClause Rewrite(WithinGroupClause node)
        {
            var rewritten = false;
            var OrderBy = TryRewrite(node.OrderBy, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new WithinGroupClause(OrderBy).WithLocation(node.Span);
        }

        protected virtual WindowSpecification Rewrite(WindowSpecification node)
        {
            var rewritten = false;
            var WithinGroup = TryRewrite(node.WithinGroup, ref rewritten);
            var Over = TryRewrite(node.Over, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new WindowSpecification(Over, WithinGroup).WithLocation(node.Span);
        }

        protected virtual EmptyStatement Rewrite(EmptyStatement node) => node;

        protected virtual BracketedExpression Rewrite(BracketedExpression node)
        {

            var rewritten = false;
            var Expression = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new BracketedExpression(Expression).WithLocation(node.Span);
        }
        protected virtual CurrentTime Rewrite(CurrentTime node) => node;

        protected virtual CurrentDate Rewrite(CurrentDate node) => node;

        protected virtual CurrentTimestamp Rewrite(CurrentTimestamp node) => node;

        protected virtual NullValue Rewrite(NullValue node) => node;

        protected virtual InListCondition Rewrite(InListCondition node)
        {
            var rewritten = false;
            var isNot = node.IsNot;
            var Left = TryRewrite(node.Left, ref rewritten);
            var Items = TryRewrite(node.Items, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new InListCondition(isNot, Left, Items).WithLocation(node.Span);
        }

        protected virtual InSelectCondition Rewrite(InSelectCondition node)
        {
            var rewritten = false;
            var isNot = node.IsNot;
            var Left = TryRewrite(node.Left, ref rewritten);
            var Expression = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new InSelectCondition(isNot, Left, Expression).WithLocation(node.Span);
        }

        protected virtual BetweenCondition Rewrite(BetweenCondition node)
        {
            var rewritten = false;
            var isNot = node.IsNot;
            var Left = TryRewrite(node.Left, ref rewritten);
            var from = TryRewrite(node.From, ref rewritten);
            var to = TryRewrite(node.To, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new BetweenCondition(isNot, Left, from, to).WithLocation(node.Span);
        }

        protected virtual DataType Rewrite(DataType node)
            => node;

        protected virtual LikeCondition Rewrite(LikeCondition node)
        {
            var rewritten = false;
            var isNot = node.IsNot;
            var Left = TryRewrite(node.Left, ref rewritten);
            var Right = TryRewrite(node.Right, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new LikeCondition(isNot, Left, Right).WithLocation(node.Span);
        }

        protected virtual DistinctValueExpression Rewrite(DistinctValueExpression node)
        {
            var rewritten = false;
            var distinctness = node.Distinctness;
            var Expression = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new DistinctValueExpression(distinctness, Expression).WithLocation(node.Span);
        }

        protected virtual ParameterIdentifier Rewrite(ParameterIdentifier node)
            => node;

        protected virtual BracketedCondition Rewrite(BracketedCondition node)
        {
            var rewritten = false;
            var con = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new BracketedCondition(con).WithLocation(node.Span);
        }

        protected virtual SelectTableReference Rewrite(SelectTableReference node)
        {
            var rewritten = false;
            var con = TryRewrite(node.Select, ref rewritten);
            var alias = node.Alias;

            if (!con.IsBracketed)
            {
                con = new SelectExpression(true, con).WithLocation(node.Span);
                rewritten = true;
            }

            if (!rewritten)
            {
                return node;
            }

            return new SelectTableReference(con, alias).WithLocation(node.Span);
        }

        protected virtual HavingClause Rewrite(HavingClause node)
        {
            var rewritten = false;
            var con = TryRewrite(node.Condition, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new HavingClause(con).WithLocation(node.Span);
        }

        protected virtual ValueFromExpression Rewrite(ValueFromExpression node)
        {
            var rewritten = false;
            var part = node.Part;
            var exp = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new ValueFromExpression(part, exp).WithLocation(node.Span);
        }

        protected virtual UnaryCondition Rewrite(UnaryCondition node)
        {
            var rewritten = false;
            var op = node.Operator;
            var right = TryRewrite(node.Right, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new UnaryCondition(op, right).WithLocation(node.Span);
        }

        protected virtual JoinClause Rewrite(JoinClause node)
        {
            var rewritten = false;
            var side = node.JoinSide;
            var type = node.JoinType;
            var TableReference = TryRewrite(node.TableReference, ref rewritten);
            var OnCondition = TryRewrite(node.OnCondition, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new JoinClause(side, type, TableReference, OnCondition).WithLocation(node.Span);
        }
        protected virtual IsNullCondition Rewrite(IsNullCondition node)
        {
            var rewritten = false;
            var expression = TryRewrite(node.Expression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new IsNullCondition(node.IsNot, expression).WithLocation(node.Span);
        }

        protected virtual CommonTableExpression Rewrite(CommonTableExpression node)
        {
            var rewritten = false;
            var body = TryRewrite(node.Body, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new CommonTableExpression(node.Alias, body).WithLocation(node.Span);
        }

        protected virtual LogicalCondition Rewrite(LogicalCondition node)
        {
            var rewritten = false;
            var left = TryRewrite(node.Left, ref rewritten);
            var right = TryRewrite(node.Right, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new LogicalCondition(left, node.Operator, right).WithLocation(node.Span);
        }

        protected virtual ExistsCondition Rewrite(ExistsCondition node)
        {
            var rewritten = false;
            var select = TryRewrite(node.SelectExpression, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new ExistsCondition(select).WithLocation(node.Span);
        }

        protected virtual WhereClause Rewrite(WhereClause node)
        {
            var rewritten = false;
            var condition = TryRewrite(node.Condition, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new WhereClause(condition).WithLocation(node.Span);
        }

        protected virtual BinaryOperatorExpression Rewrite(BinaryOperatorExpression node)
        {
            var rewritten = false;
            var left = TryRewrite(node.Left, ref rewritten);
            var right = TryRewrite(node.Right, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new BinaryOperatorExpression(left, node.Operator, right).WithLocation(node.Span);
        }

        protected virtual GroupByClause Rewrite(GroupByClause node)
        {
            var rewritten = false;
            var entries = TryRewrite(node.Entries, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new GroupByClause(entries).WithLocation(node.Span);
        }

        protected virtual StarIdentifierExpression Rewrite(StarIdentifierExpression node)
            => node;

        protected virtual AsExpression Rewrite(AsExpression node)
        {
            var rewritten = false;
            var expression = TryRewrite(node.Expression, ref rewritten);
            var DataType = TryRewrite(node.DataType, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new AsExpression(expression, DataType).WithLocation(node.Span);
        }

        protected virtual FunctionCallExpression Rewrite(FunctionCallExpression node)
        {
            var rewritten = false;
            var functionName = node.FunctionName;
            var args = TryRewrite(node.Arguments, ref rewritten);
            var window = TryRewrite(node.Window, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new FunctionCallExpression(functionName, args, window).WithLocation(node.Span);
        }

        protected virtual BinaryComparisonCondition Rewrite(BinaryComparisonCondition node)
        {
            var rewritten = false;
            var left = TryRewrite(node.Left, ref rewritten);
            var op = node.Operator;
            var right = TryRewrite(node.Right, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new BinaryComparisonCondition(left, op, right).WithLocation(node.Span);
        }

        protected virtual CaseWhenClause Rewrite(CaseWhenClause node)
        {
            var rewritten = false;
            var expression = TryRewrite(node.Expression, ref rewritten);
            var result = TryRewrite(node.Result, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new CaseWhenClause(expression, result).WithLocation(node.Span);
        }

        protected virtual CaseExpression Rewrite(CaseExpression node)
        {
            var rewritten = false;
            var expression = TryRewrite(node.Expression, ref rewritten);
            var whenClauses = TryRewrite(node.WhenClauses, ref rewritten);
            var elseExpression = TryRewrite(node.ElseResult, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new CaseExpression(expression, whenClauses, elseExpression).WithLocation(node.Span);
        }
        protected virtual OrderByEntry Rewrite(OrderByEntry node)
        {
            var rewritten = false;
            var expression = TryRewrite(node.Expression, ref rewritten);
            var dir = node.Direction;
            if (!rewritten)
            {
                return node;
            }
            return new OrderByEntry(expression, dir).WithLocation(node.Span);
        }

        protected virtual OrderByClause Rewrite(OrderByClause node)
        {
            var rewritten = false;
            var entries = TryRewrite(node.Entries, ref rewritten);
            var OffsetClause = TryRewrite(node.OffsetClause, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new OrderByClause(entries, OffsetClause).WithLocation(node.Span);
        }

        protected virtual OffsetClause Rewrite(OffsetClause node)
        {
            var rewritten = false;
            var offsetCount = TryRewrite(node.OffsetCount, ref rewritten);
            var fetchCount = TryRewrite(node.FetchCount, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new OffsetClause(offsetCount, fetchCount).WithLocation(node.Span);
        }

        protected virtual UnionStatement Rewrite(UnionStatement node)
        {
            var rewritten = false;
            var isAll = node.IsAll;
            var select = TryRewrite(node.Select, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new UnionStatement(isAll, select).WithLocation(node.Span);
        }

        protected virtual NamedTableReference Rewrite(NamedTableReference node)
        {
            var rewritten = false;
            var id = TryRewrite(node.Identifer, ref rewritten);
            var alias = node.Alias;
            if (!rewritten)
            {
                return node;
            }
            return new NamedTableReference(id, alias).WithLocation(node.Span);
        }

        protected virtual FromClause Rewrite(FromClause node)
        {
            var rewritten = false;
            var tableReferences = TryRewrite(node.TableReferences, ref rewritten);
            var joins = TryRewrite(node.Joins, ref rewritten);
            if (!rewritten)
            {
                return node;
            }
            return new FromClause(tableReferences, joins).WithLocation(node.Span);
        }

        protected virtual LiteralExpression Rewrite(LiteralExpression node)
            => node;

        protected virtual IdentifierExpression Rewrite(IdentifierExpression node)
            => node;

        protected virtual SelectExpression Rewrite(SelectExpression node)
        {
            var rewritten = false;

            var isBracketed = node.IsBracketed;
            var Top = node.Top;
            var Distinctness = node.Distinctness;
            var Projections = TryRewrite(node.Projections, ref rewritten);
            var From = TryRewrite(node.From, ref rewritten);
            var Where = TryRewrite(node.Where, ref rewritten);
            var GroupBy = TryRewrite(node.GroupBy, ref rewritten);
            var Having = TryRewrite(node.Having, ref rewritten);
            var OrderBy = TryRewrite(node.OrderBy, ref rewritten);
            var UnionStatements = TryRewrite(node.UnionStatements, ref rewritten);

            if (!rewritten)
            {
                return node;
            }

            return new SelectExpression(
                    isBracketed,
                    Top,
                    Distinctness,
                    Projections,
                    From,
                    Where,
                    GroupBy,
                    Having,
                    OrderBy,
                    UnionStatements
                ).WithLocation(node.Span);
        }

        protected virtual SelectProjection Rewrite(SelectProjection node)
        {
            var rewritten = false;

            var expression = TryRewrite(node.Expression, ref rewritten);
            var alias = node.Alias;
            var isSynthetic = node.IsSynthetic;
            if (!rewritten)
            {
                return node;
            }

            return new SelectProjection(expression, alias, isSynthetic).WithLocation(node.Span);
        }
        protected virtual SelectStatement Rewrite(SelectStatement node)
        {
            var rewritten = false;

            var cte = TryRewrite(node.CommonTableExpressions, ref rewritten);
            var selectQuery = TryRewrite(node.SelectQuery, ref rewritten);

            return rewritten ? new SelectStatement(cte, selectQuery).WithLocation(node.Span) : node;
        }

        protected virtual SqlScript Rewrite(SqlScript node)
        {
            var rewritten = false;
            var newList = TryRewrite(node.Statements, ref rewritten);
            if (rewritten)
            {
                return new SqlScript(newList).WithLocation(node.Span);
            }
            return node;
        }

        [return: NotNullIfNotNull("nodes")]
        protected IEnumerable<T>? TryRewrite<T>(IEnumerable<T>? nodes, ref bool rewritten) where T : SyntaxNode
        {
            if (nodes is null)
            {
                return null;
            }

            var rewrittenNodes = new List<T>();
            foreach (var n in nodes)
            {
                var newN = TryRewrite(n, ref rewritten);
                rewrittenNodes.Add(newN!);
            }

            return rewritten ? rewrittenNodes : nodes;
        }

        [return: NotNullIfNotNull("node")]
        protected T? TryRewrite<T>(T? node, ref bool rewritten) where T : SyntaxNode
        {
            if (node is null)
            {
                return null;
            }
            var result = Rewrite(node);
            rewritten = rewritten || !ReferenceEquals(result, node);

            return result switch
            {
                T tResult => tResult,
                _ => throw new Exception($"Unable to rewite node of {typeof(T)} to {result.GetType()}")
            };
        }
    }
}
