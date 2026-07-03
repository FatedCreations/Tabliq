using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Lexing;

namespace Tabliq.Sql.Printer;

public class SqlWriter
{
    public SqlWriter()
    {
    }

    private IndentedTextWriter? _writer;

    private string DebugString => _writer?.ToString() ?? string.Empty;

    public string ToSql(SyntaxNode node)
    {
        var writer = new IndentedTextWriter();
        _writer = writer;
        Write(node);
        _writer = null;
        return writer.ToString();
    }

    protected void Write(string val)
        => _writer?.Write(val);
    protected void WriteLine()
        => _writer?.WriteLine();

    protected void Indented(Action a)
        => _writer?.Indented(a);

    protected virtual void Write(SqlScript sqlScript)
    {
        foreach (var statement in sqlScript.Statements)
        {
            Write(statement);
        }
    }

    protected virtual void Write(Statement statement)
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

    protected virtual void Write(SelectStatement selectStatement)
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

    protected virtual void Write(CommonTableExpression commonTableExpression)
    {
        Write(commonTableExpression.Alias);
        Write(" AS (");

        Indented(() =>
        {
            WriteLine();
            Write(commonTableExpression.Body);
        });

        WriteLine();
        Write(")");
    }

    protected virtual void Write(SelectExpression select)
    {
        if (select.IsBracketed)
        {
            Write("(");
            Indented(() =>
            {
                WriteLine();
                WriteSelectBody(select);
            });
            WriteLine();
            Write(")");
        }
        else
        {
            WriteSelectBody(select);
        }

        void WriteSelectBody(SelectExpression selectExpression)
        {
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
                Indented(() =>
                {
                    WriteLine();
                    Write(selectExpression.Projections.First());
                    foreach (var projection in selectExpression.Projections.Skip(1))
                    {
                        Write(",");
                        WriteLine();
                        Write(projection);
                    }
                });
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
        }
    }

    protected virtual void Write(WhereClause whereClause)
    {
        // determine if where should start on 2nd line or not?
        Write("WHERE");

        // peek into expression to see if it is a logical condition, if so, we will write it on the next line
        if (whereClause.Condition is LogicalCondition)
        {
            Indented(() =>
            {
                WriteLine();
                Write(whereClause.Condition);
            });
        }
        else
        {
            Write(" ");
            Write(whereClause.Condition);
        }
    }
    protected virtual void Write(FromClause fromClause)
    {
        Write("FROM");

        if (fromClause.TableReferences.Count > 1)
        {
            Indented(() =>
            {
                WriteLine();
                Write(fromClause.TableReferences.First());
                foreach (var tableReference in fromClause.TableReferences.Skip(1))
                {
                    Write(",");
                    WriteLine();
                    Write(tableReference);
                }
            });
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
                Indented(() =>
                {
                    WriteLine();
                    Write("ON ");
                    Write(join.OnCondition);
                });
            }
        }
    }

    protected virtual void Write(SyntaxNode node)
    {
        switch (node)
        {
            case SqlScript sqlScript:
                Write(sqlScript);
                break;
            case SelectStatement selectStatement:
                Write(selectStatement);
                break;
            case CommonTableExpression commonTableExpression:
                Write(commonTableExpression);
                break;
            case SelectExpression selectExpression:
                Write(selectExpression);
                break;
            case FromClause fromClause:
                Write(fromClause);
                break;
            case SelectProjection selectProjection:
                Write(selectProjection);
                break;
            case NamedTableReference NamedTableReference:
                Write(NamedTableReference);
                break;
            case SelectTableReference SubqueryTableReference:
                Write(SubqueryTableReference);
                break;
            case IdentifierExpression identifierExpression:
                Write(identifierExpression);
                break;
            case StarIdentifierExpression StarIdentifierExpression:
                Write(StarIdentifierExpression);
                break;
            case BinaryComparisonCondition comparisonCondition:
                Write(comparisonCondition);
                break;
            case BracketedCondition BracketedCondition:
                Write(BracketedCondition);
                break;
            case LogicalCondition LogicalCondition:
                Write(LogicalCondition);
                break;
            case FunctionCallExpression FunctionCallExpression:
                Write(FunctionCallExpression);
                break;
            case WindowSpecification WindowSpecification:
                Write(WindowSpecification);
                break;
            case OrderByClause OrderByClause:
                Write(OrderByClause);
                break;
            case IsNullCondition IsNullCondition:
                Write(IsNullCondition);
                break;
            case LiteralExpression LiteralExpression:
                Write(LiteralExpression);
                break;
            case UnionStatement UnionStatement:
                Write(UnionStatement);
                break;
            case ValueFromExpression ValueFromExpression:
                Write(ValueFromExpression);
                break;
            case CaseExpression CaseExpression:
                Write(CaseExpression);
                break;
            case BinaryOperatorExpression BinaryOperatorExpression:
                Write(BinaryOperatorExpression);
                break;
            case AsExpression AsExpression:
                Write(AsExpression);
                break;
            case GroupByClause GroupByClause:
                Write(GroupByClause);
                break;
            case ParameterIdentifier ParameterIdentifier:
                Write(ParameterIdentifier);
                break;
            case UnaryCondition UnaryCondition:
                Write(UnaryCondition);
                break;
            case ExistsCondition ExistsCondition:
                Write(ExistsCondition);
                break;
            case DistinctValueExpression DistinctValueExpression:
                Write(DistinctValueExpression);
                break;
            case LikeCondition LikeCondition:
                Write(LikeCondition);
                break;
            case DataType type:
                Write(type);
                break;
            case BetweenCondition BetweenCondition:
                Write(BetweenCondition);
                break;
            case InSelectCondition InSelectCondition:
                Write(InSelectCondition);
                break;
            case InListCondition InListCondition:
                Write(InListCondition);
                break;
            case CurrentDate CurrentDate:
                Write(CurrentDate);
                break;
            case CurrentTimestamp CurrentTimestamp:
                Write(CurrentTimestamp);
                break;
            case NullValue NullValue:
                Write(NullValue);
                break;
            case CurrentTime CurrentTime:
                Write(CurrentTime);
                break;
            default:
                throw new NotImplementedException($"Writing for {node.GetType().Name} is not implemented.");
        }
    }

    protected virtual void Write(CurrentDate val)
    {
        Write("CURRENT_DATE");
    }

    protected virtual void Write(CurrentTimestamp val)
    {
        Write("CURRENT_TIMESTAMP");
    }

    protected virtual void Write(CurrentTime val)
    {
        Write("CURRENT_TIME");
    }

    protected virtual void Write(NullValue val)
    {
        Write("NULL");
    }
    protected virtual void Write(InSelectCondition val)
    {
        Write(val.Left);
        if (val.IsNot)
        {
            Write(" NOT");
        }
        Write(" IN ");
        Write(val.Expression);
    }

    protected virtual void Write(InListCondition val)
    {
        Write(val.Left);
        if (val.IsNot)
        {
            Write(" NOT");
        }
        Write(" IN (");
        Write(val.Items.First());
        foreach (var itm in val.Items.Skip(1))
        {
            Write(", ");
            Write(itm);
        }
        Write(")");
    }

    protected virtual void Write(BetweenCondition val)
    {
        Write(val.Left);
        if (val.IsNot)
        {
            Write(" NOT");
        }
        Write(" BETWEEN ");
        Write(val.From);
        Write(" AND ");
        Write(val.To);
    }

    protected virtual void Write(DataType val)
    {
        Write(val.Name);
        if (!string.IsNullOrEmpty(val.Size))
        {
            Write("(");
            Write(val.Size);
            Write(")");
        }
    }

    protected virtual void Write(DistinctValueExpression val)
    {
        Write(val.Distinctness switch
        {
            Distinctness.Distinct => "DISTINCT ",
            Distinctness.All => "ALL ",
            _ => ""
        });
        Write(val.Expression);
    }

    protected virtual void Write(ExistsCondition existsCondition)
    {
        Write("EXISTS (");
        Indented(() =>
        {
            WriteLine();
            Write(existsCondition.SelectExpression);
        });
        WriteLine();
        Write(")");
    }

    protected virtual void Write(UnaryCondition unaryCondition)
    {
        Write(unaryCondition.Operator switch
        {
            UnaryCompararisonOperator.Not => "NOT ",
            _ => throw new NotImplementedException($"Operator {unaryCondition.Operator} is not implemented.")
        });

        Write(unaryCondition.Right);
    }

    protected virtual void Write(ParameterIdentifier parameterIdentifier)
    {
        Write("@");
        Write(parameterIdentifier.ParamterName);
    }
    protected virtual void Write(GroupByClause groupByClause)
    {
        Write("GROUP BY");
        if (groupByClause.Entries.Count == 1)
        {
            Write(" ");
            Write(groupByClause.Entries[0]);
        }
        else
        {
            Indented(() =>
            {
                WriteLine();
                Write(groupByClause.Entries.First());

                foreach (var entry in groupByClause.Entries.Skip(1))
                {
                    Write(",");
                    WriteLine();
                    Write(entry);
                }
            });
        }
    }
    protected virtual void Write(HavingClause having)
    {
        // determine if having should start on 2nd line or not?
        Write("HAVING");

        // peek into expression to see if it is a logical condition, if so, we will write it on the next line
        if (having.Condition is LogicalCondition)
        {
            Indented(() =>
            {
                WriteLine();
                Write(having.Condition);
            });
        }
        else
        {
            Write(" ");
            Write(having.Condition);
        }
    }

    protected virtual void Write(AsExpression asExpression)
    {
        Write(asExpression.Expression);
        Write(" AS ");
        Write(asExpression.DataType);
    }

    protected virtual void Write(BinaryOperatorExpression binaryOperatorExpression)
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

    protected virtual void Write(CaseExpression caseExpression)
    {
        Write("CASE");

        if (caseExpression.Expression is not null)
        {
            Write(" ");
            Write(caseExpression.Expression);
        }

        Indented(() =>
        {
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
        });
        WriteLine();
        Write("END");
    }
    protected virtual void Write(ValueFromExpression valueFromExpression)
    {
        Write(valueFromExpression.Part);
        Write(" FROM ");
        Write(valueFromExpression.Expression);
    }
    protected virtual void Write(UnionStatement union)
    {
        Write("UNION");
        if (union.IsAll)
        {
            Write(" ALL");
        }

        WriteLine();

        Write(union.Select);
    }
    protected virtual void Write(LiteralExpression lit)
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
    protected virtual void Write(IsNullCondition isNulls)
    {
        Write(isNulls.Expression);
        Write(" IS ");
        if (isNulls.IsNot)
        {
            Write("NOT ");
        }
        Write("NULL");
    }

    protected virtual void Write(OrderByClause order, bool inline = false)
    {
        Write("ORDER BY");

        if (order.Entries.Count == 1)
        {
            inline = true;
        }

        if (inline)
        {
            Write(" ");
            WriteBody();
        }
        else
        {
            Indented(() =>
            {
                WriteLine();
                WriteBody();
            });
        }

        void WriteBody()
        {
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

            if (order.OffsetClause is not null)
            {
                Write(" ");
                Write(order.OffsetClause);
            }
        }
    }

    protected virtual void Write(OffsetClause offset)
    {
        Write("OFFSET ");
        Write(offset.OffsetCount);
        Write(" ROWS");
        if (offset.FetchCount is not null)
        {
            Write(" FETCH NEXT ");
            Write(offset.FetchCount);
            Write(" ROWS ONLY");
        }
    }

    protected virtual void Write(OrderByEntry entry)
    {
        Write(entry.Expression);
        if (entry.Direction != OrderByDirection.Unspecified)
        {
            Write(" ");
            Write(entry.Direction == OrderByDirection.Ascending ? "ASC" : "DESC");
        }
    }

    protected virtual void Write(FunctionCallExpression func)
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
    protected virtual void Write(WindowSpecification window)
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

    protected virtual void Write(BracketedCondition BracketedCondition)
    {
        Write("(");
        Indented(() =>
        {
            WriteLine();
            Write(BracketedCondition.Expression);
        });
        WriteLine();
        Write(")");
    }
    protected virtual void Write(BinaryComparisonCondition comparisonCondition)
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
            _ => throw new NotImplementedException($"Operator {comparisonCondition.Operator} is not implemented.")
        });
        Write(" ");
        Write(comparisonCondition.Right);
    }

    protected virtual void Write(LikeCondition comparisonCondition)
    {
        Write(comparisonCondition.Left);
        if (comparisonCondition.IsNot)
        {
            Write(" NOT");
        }
        Write(" ");
        Write("LIKE");
        Write(" ");
        Write(comparisonCondition.Right);
    }

    protected virtual bool RequiresQuotedIdentifier(string name)
    {
        if (name == "*")
        {
            return false;
        }

        if (Lexer.GetKeywordKind(name) != SyntaxKind.IdentifierToken)
        {
            return true;
        }
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '@'))
            {
                return true;
            }
        }
        return false;
    }

    protected virtual string QuoteIdentifierPartIfNeeded(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
        if (RequiresQuotedIdentifier(name))
        {
            var esc = name.Replace("]", "]]");
            return "[" + esc + "]";
        }
        return name;
    }

    protected virtual void Write(StarIdentifierExpression identifier)
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
    protected virtual void Write(IdentifierExpression identifier)
    {
        Write(QuoteIdentifierPartIfNeeded(identifier.IdentifierParts.First()));
        foreach (var part in identifier.IdentifierParts.Skip(1))
        {
            Write(".");
            Write(QuoteIdentifierPartIfNeeded(part));
        }
    }
    protected virtual void Write(NamedTableReference table)
    {
        Write(table.Identifer);
        if (table.Alias is not null)
        {
            Write(" AS ");
            Write(QuoteIdentifierPartIfNeeded(table.Alias));
        }
    }

    protected virtual void Write(SelectTableReference table)
    {
        Write(table.Select);
        if (table.Alias is not null)
        {
            Write(" AS ");
            Write(QuoteIdentifierPartIfNeeded(table.Alias));
        }
    }

    protected virtual void Write(LogicalCondition con)
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

    protected virtual void Write(SelectProjection selectProjection)
    {
        Write(selectProjection.Expression);
        if (selectProjection.Alias is not null && selectProjection.IsSynthetic == false)
        {
            Write(" AS ");
            Write(QuoteIdentifierPartIfNeeded(selectProjection.Alias));
        }
    }
}
