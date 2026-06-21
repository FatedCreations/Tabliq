using System.Text;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Printer
{
    public class SqlWriter
    {
        private readonly StringBuilder _sb;
        private readonly SyntaxNode _root;

        public static string ToSql(SyntaxNode node)
        {
            var writer = new SqlWriter(node);
            return writer.ToString();
        }

        private string DebugString => _sb.ToString();

        private SqlWriter(SyntaxNode node)
        {
            _sb = new StringBuilder();
            _root = node;
            Write(node);
            // walk the tree here and build the SQL string representation
        }

        private void Write(SqlScript sqlScript)
        {
            foreach (var statement in sqlScript.Statements)
            {
                Write(statement);
            }
        }

        private void Write(Statement statement)
        {
            if (statement is SelectStatement selectStatement)
            {
                Write(selectStatement);
            }
            else
            {
                throw new NotImplementedException($"Writing for {statement.GetType().Name} is not implemented.");
            }
        }

        private void Write(SelectStatement selectStatement)
        {
            if (selectStatement.CommonTableExpressions.Any())
            {
                Write("WITH ");

                Write(selectStatement.CommonTableExpressions.First());

                foreach (var cte in selectStatement.CommonTableExpressions.Skip(1))
                {
                    Write(", ");
                    WriteLine();
                    Write(cte);
                }
                WriteLine();
            }
            Write(selectStatement.SelectQuery);
        }

        private void Write(CommonTableExpression commonTableExpression)
        {
            Write(commonTableExpression.Alias);
            Write(" AS (");
            Indent();
            WriteLine();

            Write(commonTableExpression.Body);

            Outdent();
            WriteLine();

            Write(")");
        }

        private void Write(SelectExpression selectExpression)
        {
            if (selectExpression.IsBracketed)
            {
                Write("(");
                Indent();
                WriteLine();
            }

            Write("SELECT");

            if (selectExpression.Top.HasValue)
            {
                Write($" TOP {selectExpression.Top.Value}");
            }

            if (selectExpression.Distinctness == Distinctness.All)
            {
                Write(" ALL");
            }
            else if (selectExpression.Distinctness == Distinctness.Distinct)
            {
                Write(" DISTINCT");
            }

            if (selectExpression.Projections.Count > 1)
            {
                Indent();
                WriteLine();
                Write(selectExpression.Projections.First());
                foreach (var projection in selectExpression.Projections.Skip(1))
                {
                    Write(",");
                    WriteLine();
                    Write(projection);
                }
                Outdent();
            }
            else
            {
                Write(" ");
                Write(selectExpression.Projections.First());
            }

            // from clause
            if (selectExpression.From is not null)
            {
                WriteLine();
                Write(selectExpression.From);
            }
            // where clause
            if (selectExpression.Where is not null)
            {
                WriteLine();
                Write(selectExpression.Where);
            }
            //grouo by
            // having

            if (selectExpression.GroupBy is not null)
            {
                WriteLine();
                Write(selectExpression.GroupBy);
            }
            if (selectExpression.Having is not null)
            {
                WriteLine();
                Write(selectExpression.Having);
            }
            if (selectExpression.OrderBy is not null)
            {
                WriteLine();
                Write(selectExpression.OrderBy);
            }

            foreach (var union in selectExpression.UnionStatements)
            {
                WriteLine();
                Write(union);
            }

            if (selectExpression.IsBracketed)
            {
                Outdent();
                WriteLine();
                Write(")");
            }
        }
        private void Write(WhereClause whereClause)
        {
            // determine if where should start on 2nd line or not?
            Write("WHERE");

            // peek into expression to see if it is a logical condition, if so, we will write it on the next line
            bool indented = false;
            if (whereClause.Condition is LogicalCondition)
            {
                indented = true;
                Indent();
                WriteLine();
            }
            else
            {
                Write(" ");
            }

            Write(whereClause.Condition);
            if (indented)
            {
                Outdent();
            }
        }
        private void Write(FromClause fromClause)
        {
            Write("FROM");

            if (fromClause.TableReferences.Count > 1)
            {
                Indent();
                WriteLine();
                Write(fromClause.TableReferences.First());
                foreach (var tableReference in fromClause.TableReferences.Skip(1))
                {
                    Write(",");
                    WriteLine();
                    Write(tableReference);
                }
                Outdent();
                WriteLine();
            }
            else
            {
                Write(" ");
                Write(fromClause.TableReferences.First());
            }
            // joins 1 per line
            foreach (var join in fromClause.Joins)
            {
                WriteLine();

                Write(join.JoinSide switch
                {
                    JoinSide.Left => "LEFT ",
                    JoinSide.Right => "RIGHT ",
                    JoinSide.Full => "FULL ",
                    _ => ""
                });

                Write(join.JoinType switch
                {
                    JoinType.Inner => "INNER ",
                    JoinType.Outer => "OUTER ",
                    JoinType.Cross => "CROSS ",
                    _ => ""
                });
                Write("JOIN ");
                Write(join.TableReference);

                if (join.OnCondition is not null)
                {
                    Indent();
                    WriteLine();
                    Write("ON ");
                    Write(join.OnCondition);
                    Outdent();
                }
            }
        }

        void Write(SyntaxNode node)
        {
            // big if else chain to handle different expression types

            if (node is SqlScript sqlScript)
            {
                Write(sqlScript);
            }
            else if (node is Statement statement)
            {
                Write(statement);
            }
            else if (node is SelectStatement selectStatement)
            {
                Write(selectStatement);
            }
            else if (node is CommonTableExpression commonTableExpression)
            {
                Write(commonTableExpression);
            }
            else if (node is SelectExpression selectExpression)
            {
                Write(selectExpression);
            }
            else if (node is FromClause fromClause)
            {
                Write(fromClause);
            }
            else if (node is SelectProjection selectProjection)
            {
                Write(selectProjection);
            }
            else if (node is NamedTableReference NamedTableReference)
            {
                Write(NamedTableReference);
            }
            else if (node is SelectTableReference SubqueryTableReference)
            {
                Write(SubqueryTableReference);
            }
            else if (node is IdentifierExpression identifierExpression)
            {
                Write(identifierExpression);
            }
            else if (node is StarIdentifierExpression StarIdentifierExpression)
            {
                Write(StarIdentifierExpression);
            }

            else if (node is BinaryComparisonCondition comparisonCondition)
            {
                Write(comparisonCondition);
            }
            else if (node is BracketedCondition BracketedCondition)
            {
                Write(BracketedCondition);
            }
            else if (node is LogicalCondition LogicalCondition)
            {
                Write(LogicalCondition);
            }
            else if (node is FunctionCallExpression FunctionCallExpression)
            {
                Write(FunctionCallExpression);
            }
            else if (node is WindowSpecification WindowSpecification)
            {
                Write(WindowSpecification);
            }
            else if (node is OrderByClause OrderByClause)
            {
                Write(OrderByClause);
            }
            else if (node is IsNullCondition IsNullCondition)
            {
                Write(IsNullCondition);
            }
            else if (node is LiteralExpression LiteralExpression)
            {
                Write(LiteralExpression);
            }
            else if (node is UnionStatement UnionStatement)
            {
                Write(UnionStatement);
            }
            else if (node is ValueFromExpression ValueFromExpression)
            {
                Write(ValueFromExpression);
            }
            else if (node is CaseExpression CaseExpression)
            {
                Write(CaseExpression);
            }
            else if (node is BinaryOperatorExpression BinaryOperatorExpression)
            {
                Write(BinaryOperatorExpression);
            }
            else if (node is AsExpression AsExpression)
            {
                Write(AsExpression);
            }
            else if (node is GroupByClause GroupByClause)
            {
                Write(GroupByClause);
            }
            else if (node is ParameterIdentifier ParameterIdentifier)
            {
                Write(ParameterIdentifier);
            }
            else if (node is UnaryCondition UnaryCondition)
            {
                Write(UnaryCondition);
            }
            else if (node is ExistsCondition ExistsCondition)
            {
                Write(ExistsCondition);
            }
            else
            {
                throw new NotImplementedException($"Writing for {node.GetType().Name} is not implemented.");
            }
        }
        void Write(ExistsCondition existsCondition)
        {
            Write("EXISTS (");
            Indent();
            WriteLine();
            Write(existsCondition.SelectExpression);
            Outdent();
            WriteLine();
            Write(")");
        }
        void Write(UnaryCondition unaryCondition)
        {
            Write(unaryCondition.Operator switch
            {
                UnaryCompararisonOperator.Not => "NOT ",
                _ => throw new NotImplementedException($"Operator {unaryCondition.Operator} is not implemented.")
            });

            Write(unaryCondition.Right);
        }

        void Write(ParameterIdentifier parameterIdentifier)
        {
            Write("@");
            Write(parameterIdentifier.ParamterName);
        }
        void Write(GroupByClause groupByClause)
        {
            Write("GROUP BY");
            if (groupByClause.Entries.Count == 1)
            {
                Write(" ");
                Write(groupByClause.Entries[0]);
            }
            else
            {
                Indent();

                WriteLine();
                Write(groupByClause.Entries.First());

                foreach (var entry in groupByClause.Entries.Skip(1))
                {
                    Write(",");
                    WriteLine();
                    Write(entry);
                }
                Outdent();
            }
        }
        void Write(HavingClause having)
        {
            // determine if having should start on 2nd line or not?
            Write("HAVING");

            // peek into expression to see if it is a logical condition, if so, we will write it on the next line
            bool indented = false;
            if (having.Condition is LogicalCondition)
            {
                indented = true;
                Indent();
                WriteLine();
            }
            else
            {
                Write(" ");
            }

            Write(having.Condition);
            if (indented)
            {
                Outdent();
            }
        }

        void Write(AsExpression asExpression)
        {
            Write(asExpression.Expression);
            Write(" AS ");
            Write(asExpression.Alias);
        }

        void Write(BinaryOperatorExpression binaryOperatorExpression)
        {
            Write(binaryOperatorExpression.Left);
            Write(" ");
            Write(binaryOperatorExpression.Operator switch
            {
                BinaryOperator.Add => "+",
                BinaryOperator.Subtract => "-",
                BinaryOperator.Multiply => "*",
                BinaryOperator.Divide => "/",
                BinaryOperator.Modulus => "%",
                BinaryOperator.Concatenate => "||",
                _ => throw new NotImplementedException($"Operator {binaryOperatorExpression.Operator} is not implemented.")
            });
            Write(" ");
            Write(binaryOperatorExpression.Right);
        }

        void Write(CaseExpression caseExpression)
        {
            Write("CASE");

            if (caseExpression.Expression is not null)
            {
                Write(" ");
                Write(caseExpression.Expression);
            }

            Indent();

            foreach (var whenClause in caseExpression.WhenClauses)
            {
                WriteLine();
                Write("WHEN ");
                Write(whenClause.Expression);
                Write(" THEN ");
                Write(whenClause.Result);
            }

            if (caseExpression.ElseResult is not null)
            {
                WriteLine();
                Write("ELSE ");
                Write(caseExpression.ElseResult);
            }

            Outdent();
            WriteLine();
            Write("END");
        }
        void Write(ValueFromExpression valueFromExpression)
        {
            Write(valueFromExpression.Part);
            Write(" FROM ");
            Write(valueFromExpression.Expression);
        }
        void Write(UnionStatement union)
        {
            Write("UNION");
            if (union.IsAll)
            {
                Write(" ALL");
            }

            WriteLine();

            Write(union.Select);
        }
        void Write(LiteralExpression lit)
        {
            if (lit.Value is null)
            {
                Write("NULL");
            }
            else if (lit.Value is string s)
            {
                Write($"'{s.Replace("'", "''")}'");
            }
            else if (lit.Value is long l)
            {
                Write(l.ToString());
            }
            else if (lit.Value is double d)
            {
                var val = d.ToString();
                Write(val);
                if (!val.Contains('.')) // force the decimal place if got one in the ast 
                {
                    Write(".0");
                }
            }
            else
            {
                Write($"{lit.Value}"); // fallback
            }
        }
        void Write(IsNullCondition isNulls)
        {
            Write(isNulls.Expression);
            Write(" IS ");
            if (isNulls.IsNot)
            {
                Write("NOT ");
            }
            Write("NULL");
        }

        void Write(OrderByClause order, bool inline = false)
        {
            Write("ORDER BY");

            if (order.Entries.Count == 1)
            {
                inline = true;
            }

            if (inline)
            {
                Write(" ");
            }
            else
            {
                Indent();
                WriteLine();
            }

            Write(order.Entries.First());
            foreach (var entry in order.Entries.Skip(1))
            {
                Write(",");
                if (inline)
                {
                    Write(" ");
                }
                else
                {
                    WriteLine();
                }
                Write(entry);
            }
        }

        void Write(OrderByEntry entry)
        {
            Write(entry.Expression);
            if (entry.Direction != OrderByDirection.Unspecified)
            {
                Write(" ");
                Write(entry.Direction == OrderByDirection.Ascending ? "ASC" : "DESC");
            }
        }

        void Write(FunctionCallExpression func)
        {
            Write(func.FunctionName.ToUpperInvariant());
            Write("(");

            if (func.Arguments.Any())
            {
                Write(func.Arguments.First());
                foreach (var arg in func.Arguments.Skip(1))
                {
                    Write(", ");
                    Write(arg);
                }
            }
            Write(")");

            if (func.Window is not null)
            {
                Write(" ");
                Write(func.Window);
            }
        }
        void Write(WindowSpecification window)
        {
            Write("OVER ");
            Write("(");
            if (window.Partions.Any())
            {
                Write("PARTITION BY ");
                Write(window.Partions.First());
                foreach (var partition in window.Partions.Skip(1))
                {
                    Write(", ");
                    Write(partition);
                }
            }
            if (window.OrderBy is not null)
            {
                Write(" ");
                Write(window.OrderBy, true);
            }
            Write(")");
        }

        void Write(BracketedCondition BracketedCondition)
        {
            Write("(");
            Indent();
            WriteLine();
            Write(BracketedCondition.Expression);

            Outdent();
            WriteLine();
            Write(")");
        }
        void Write(BinaryComparisonCondition comparisonCondition)
        {
            Write(comparisonCondition.Left);
            Write(" ");
            Write(comparisonCondition.Operator switch
            {
                BinaryCompararisonOperator.Equals => "=",
                BinaryCompararisonOperator.NotEquals => "<>",
                BinaryCompararisonOperator.GreaterThan => ">",
                BinaryCompararisonOperator.LessThan => "<",
                BinaryCompararisonOperator.GreaterThanOrEqual => ">=",
                BinaryCompararisonOperator.LessThanOrEqual => "<=",
                BinaryCompararisonOperator.Like => "LIKE",
                _ => throw new NotImplementedException($"Operator {comparisonCondition.Operator} is not implemented.")
            });
            Write(" ");
            Write(comparisonCondition.Right);
        }

        public static string QuoteIdentifierPartIfNeeded(string? name)
        {
            if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
            // preserve wildcard/star tokens
            if (name == "*" || name.EndsWith(".*", StringComparison.Ordinal)) return name;
            // if name already looks safe (letters, digits, underscore, dot, @) print as-is
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '@'))
                {
                    // needs bracket quoting; escape closing bracket by doubling
                    var esc = name.Replace("]", "]]");
                    return "[" + esc + "]";
                }
            }

            return name;
        }

        void Write(StarIdentifierExpression identifier)
        {
            if (identifier.IdentifierParts.Any())
            {
                foreach (var part in identifier.IdentifierParts)
                {
                    Write(QuoteIdentifierPartIfNeeded(part));
                    Write(".");
                }
            }
            Write("*");
        }
        void Write(IdentifierExpression identifier)
        {
            Write(QuoteIdentifierPartIfNeeded(identifier.IdentifierParts.First()));
            foreach (var part in identifier.IdentifierParts.Skip(1))
            {
                Write(".");
                Write(QuoteIdentifierPartIfNeeded(part));
            }
        }
        void Write(NamedTableReference table)
        {
            Write(table.Identifer);
            if (table.Alias is not null)
            {
                Write(" AS ");
                Write(QuoteIdentifierPartIfNeeded(table.Alias));
            }
        }

        void Write(SelectTableReference table)
        {
            Write(table.Select);
            if (table.Alias is not null)
            {
                Write(" AS ");
                Write(QuoteIdentifierPartIfNeeded(table.Alias));
            }
        }
        void Write(LogicalCondition con)
        {
            Write(con.Left);
            Write(con.Operator switch
            {
                LogicalOperator.And => " AND",
                LogicalOperator.Or => " OR",
                _ => throw new NotImplementedException($"Operator {con.Operator} is not implemented.")
            });
            WriteLine();
            Write(con.Right);
        }

        void Write(SelectProjection selectProjection)
        {
            Write(selectProjection.Expression);
            if (selectProjection.Alias is not null && selectProjection.IsSynthetic == false)
            {
                Write(" AS ");
                Write(selectProjection.Alias);
            }
        }

        private int indentLevel = 0;
        private void Indent()
        {
            indentLevel++;
        }
        private void Outdent()
        {
            indentLevel--;
        }

        private void WriteLine()
        {
            _sb.AppendLine();
            for (int i = 0; i < indentLevel; i++)
            {
                _sb.Append("    "); // Assuming 4 spaces per indent level
            }
        }

        private void Write(string value)
        {
            _sb.Append(value);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
