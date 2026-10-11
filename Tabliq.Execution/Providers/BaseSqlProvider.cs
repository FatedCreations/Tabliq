using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Execution.Functions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Execution.Providers;

public abstract class RemoteSqlProviderBase : IExecutionProvider
{
    public virtual TableSymbol? GetTable(string tableName, string? schemaName = null)
        => GetTables().FirstOrDefault(x => x.IsMatch(tableName, schemaName));

    public abstract IEnumerable<TableSymbol> GetTables();

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node, ExecutionRewriteContext? context = null)
        => node switch
        {
            OffsetFetchExecutionPlanNode p when p.Input is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => OffsetFetchExecutionPlanNode(p, s, context),
            SortedeExecutionPlanNode p when p.Input is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => SortedeExecutionPlanNode(p, s, context),
            UnionAllExecutionPlanNode p when p.Input1 is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this && p.Input2 is RemoteSqProviderSqlExecutionPlanNode r && r.Provider == this => UnionAllExecutionPlanNode(p, s, r, context),
            JoinExecutionPlanNode p when p.Left is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this && p.Right is RemoteSqProviderSqlExecutionPlanNode r && r.Provider == this => JoinExecutionPlanNode(p, s, r, context),
            FilterExecutionPlanNode p when p.Input is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => FilterExecutionPlanNode(p, s, context),
            SubqueryExecutionPlanNode p when p.Inner is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => SubqueryExecutionPlanNode(p, s, context),
            GroupByAggregateExecutionPlanNode p when p.Input is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => GroupByAggregateExecutionPlanNode(p, s, context),
            ProjectionExecutionPlanNode p when p.Input is RemoteSqProviderSqlExecutionPlanNode s && s.Provider == this => ProjectionExecutionPlanNode(p, s, context),
            TableScanExecutionPlanNode t => TableScanExecutionPlanNode(t, context),
            _ => node
        };

    public ExecutionPlanNode TableScanExecutionPlanNode(TableScanExecutionPlanNode node, ExecutionRewriteContext? context = null)
    {
        var aliasName = node.TableName != node.Alias ? node.Alias : null;
        var sql = new SelectStatement([]
        , new SelectExpression(
            false,
            null,
            Distinctness.Unspecified,
            node.Columns.Distinct().Select(c => new SelectProjection(new IdentifierExpression(aliasName, c.Name)
            {
                Binding = new ColumnBinding(new TableBinding(aliasName, node.Table), c)
            }, c.Name)).ToList(),
            new FromClause([new NamedTableReference(new IdentifierExpression(node.TableName), aliasName) {
                Binding = node.Table
            }], []), null, null, null, null, []));

        return new RemoteSqProviderSqlExecutionPlanNode(this, sql, null)
        {
            TableScan = true,
            Alias = node.Alias
        };
    }

    public ExecutionPlanNode OffsetFetchExecutionPlanNode(OffsetFetchExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        var p = parent.SqlStatement;

        // we are already in an ordered context user fetch and offset, we can just add the new order bys to the existing order bys and add the new offset and fetch to the existing offset and fetch
        long? top = p.SelectQuery.Top;
        OrderByClause? order = p.SelectQuery.OrderBy;
        if (node.Offset is null && p.SelectQuery.OrderBy is null && node.Fetch is LiteralExpressionExecutionPlan lit && lit.Value is long t)
        {
            top = t;
        }
        else
        {
            Expression? offset = null;
            Expression? fetch = null;

            if (node.Offset is not null && !TryConvert(node.Offset, context, out offset))
            {
                context?.Report("UnsupportedExpressionForPushdown", $"Expression {node.Offset} is not supported for pushdown");
                return node;
            }

            if (node.Fetch is not null && !TryConvert(node.Fetch, context, out fetch))
            {
                context?.Report("UnsupportedExpressionForPushdown", $"Expression {node.Fetch} is not supported for pushdown");
                return node;
            }

            order = new OrderByClause(p.SelectQuery.OrderBy?.Entries ?? [], new OffsetClause(offset, fetch));
        }

        var newSelect = new SelectStatement(p.CommonTableExpressions, new SelectExpression(
            false,
            top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            p.SelectQuery.From,
            p.SelectQuery.Where,
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            order,
            p.SelectQuery.UnionStatements));

        return new RemoteSqProviderSqlExecutionPlanNode(this, newSelect, parent.Parameters);
    }
    public ExecutionPlanNode SortedeExecutionPlanNode(SortedeExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        var p = parent.SqlStatement;

        List<OrderByEntry> entries = new List<OrderByEntry>();

        foreach (var col in node.Columns)
        {
            if (TryConvert(col.Expression, context, out var expression))
            {
                entries.Add(new OrderByEntry(expression, col.Ascending ? OrderByDirection.Ascending : OrderByDirection.Descending));
            }
            else
            {
                context?.Report("UnsupportedExpressionForPushdown", $"Expression {col.Expression} is not supported for pushdown");
                return node;
            }
        }

        var newSelect = new SelectStatement(parent.SqlStatement.CommonTableExpressions, new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            p.SelectQuery.From,
            p.SelectQuery.Where,
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            new OrderByClause([.. p.SelectQuery.OrderBy?.Entries ?? [], .. entries], p.SelectQuery.OrderBy?.OffsetClause),
            p.SelectQuery.UnionStatements));

        return new RemoteSqProviderSqlExecutionPlanNode(this, newSelect, parent.Parameters);
    }

    public ExecutionPlanNode UnionAllExecutionPlanNode(UnionAllExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode left, RemoteSqProviderSqlExecutionPlanNode right, ExecutionRewriteContext? context = null)
    {
        IEnumerable<CommonTableExpression> ctes = [.. left.SqlStatement.CommonTableExpressions, .. right.SqlStatement.CommonTableExpressions];

        var p = left.SqlStatement;

        var newSelect = new SelectStatement(ctes.Distinct(), new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            p.SelectQuery.From,
            p.SelectQuery.Where,
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            p.SelectQuery.OrderBy,
            [.. p.SelectQuery.UnionStatements, new UnionStatement(true, right.SqlStatement.SelectQuery)]));

        List<ExecuterParameter>? parameters = null;
        if (left.Parameters is not null)
        {
            parameters ??= [];
            parameters.AddRange(left.Parameters);
        }
        if (right.Parameters is not null)
        {
            parameters ??= [];
            parameters.AddRange(right.Parameters);
        }

        return new RemoteSqProviderSqlExecutionPlanNode(this, newSelect, parameters?.Distinct());
    }


    public ExecutionPlanNode JoinExecutionPlanNode(JoinExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode left, RemoteSqProviderSqlExecutionPlanNode right, ExecutionRewriteContext? context = null)
    {
        Condition? condition = null;

        if (node.OnConditionPlan is not null && !TryConvert(node.OnConditionPlan, context, out condition))
        {
            context?.Report("UnsupportedExpressionForPushdown", $"Expression {node.OnConditionPlan} is not supported for pushdown");
            return node;
        }

        var p = left.SqlStatement;

        if (!right.TableScan)
        {
            return node; // we can't push down a join if the right side is not a table scan, we need to return the node as is
        }

        var tableRef = right.SqlStatement.SelectQuery.From?.TableReferences.FirstOrDefault() ?? throw new InvalidOperationException("Right side of join must have a table reference");

        var joinClause = new JoinClause(
            node.JoinSide,
            node.JoinType,
             tableRef,
            condition);

        var from = new FromClause(p.SelectQuery.From?.TableReferences ?? [],
            [
                ..p.SelectQuery.From?.Joins ?? [],
                joinClause
            ]);

        var result = new SelectStatement([..p.CommonTableExpressions, ..right.SqlStatement.CommonTableExpressions], new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            from,
            p.SelectQuery.Where,
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            p.SelectQuery.OrderBy,
            p.SelectQuery.UnionStatements));


        List<ExecuterParameter>? parameters = null;
        if (left.Parameters is not null)
        {
            parameters ??= [];
            parameters.AddRange(left.Parameters);
        }
        if (right.Parameters is not null)
        {
            parameters ??= [];
            parameters.AddRange(right.Parameters);
        }
        return new RemoteSqProviderSqlExecutionPlanNode(this, result, parameters?.Distinct());
    }

    public ExecutionPlanNode FilterExecutionPlanNode(FilterExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        if (!TryConvert(node.ConditionPlan, context, out var condition))
        {
            context?.Report("UnsupportedExpressionForPushdown", $"Expression {node.ConditionPlan} is not supported for pushdown");
            return node;
        }

        var p = parent.SqlStatement;

        if (p.SelectQuery.Where?.Condition is not null)
        {
            new LogicalCondition(p.SelectQuery.Where.Condition, LogicalOperator.And, condition);
        }

        var result = new SelectStatement(p.CommonTableExpressions, new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            p.SelectQuery.From,
            new WhereClause(condition),
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            p.SelectQuery.OrderBy,
            p.SelectQuery.UnionStatements));

        return new RemoteSqProviderSqlExecutionPlanNode(this, result, parent.Parameters);
    }
    public ExecutionPlanNode SubqueryExecutionPlanNode(SubqueryExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        List<string> aliases = new List<string>();
        foreach (var p in parent.SqlStatement.SelectQuery.Projections)
        {
            var alias = p.Alias;
            if (alias is null && p.Expression is IdentifierExpression i)
            {
                alias = i.Column;
            }

            if (string.IsNullOrEmpty(alias))
            {
                context?.Report("MissingAlias", $"Projection {p.Expression} is missing an alias, cannot push down subquery");
                return node;
            }
            aliases.Add(alias);
        }

        var projections = aliases.Select(x => new SelectProjection(new IdentifierExpression(node.Alias ?? node.CteName, x)));

        if (node.IsCte)
        {
            List<CommonTableExpression>? commonTableExpressions = [.. parent.SqlStatement.CommonTableExpressions];
            commonTableExpressions.Add(new CommonTableExpression(node.CteName!, parent.SqlStatement.SelectQuery));

            var selectStatement = new SelectStatement(commonTableExpressions, new SelectExpression(
                false,
                null,
                Distinctness.Unspecified,
                projections,
                new FromClause([new NamedTableReference(new IdentifierExpression(node.CteName!), node.Alias)], []),
                null,
                null,
                null,
                null,
                []));

            return new RemoteSqProviderSqlExecutionPlanNode(this, selectStatement, parent.Parameters)
            {
                TableScan = true
            };
        }
        else
        {
            var selectStatement = new SelectStatement(parent.SqlStatement.CommonTableExpressions, new SelectExpression(
                false,
                null,
                Distinctness.Unspecified,
                projections,
                new FromClause([new SelectTableReference(new SelectExpression(true, parent.SqlStatement.SelectQuery), node.Alias)], []),
                null,
                null,
                null,
                null,
                []));

            return new RemoteSqProviderSqlExecutionPlanNode(this, selectStatement, parent.Parameters)
            {
                TableScan = true
            };
        }
    }
    public ExecutionPlanNode GroupByAggregateExecutionPlanNode(GroupByAggregateExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        var p = parent.SqlStatement;

        List<Expression> boundGroup = new List<Expression>();
        List<ExpressionPlanNode> missedGroup = new List<ExpressionPlanNode>();

        foreach (var col in node.Groups)
        {
            if (TryConvert(col, context, out var expression))
            {
                boundGroup.Add(expression);
            }
            else
            {
                return node; // incompatable aggregate the whole phase move to in memory?
            }
        }

        foreach (var col in node.Aggregates)
        {
            // can't proce
            if (!TryConvert(col, context, out var _))
            {
                return node; // incompatable aggregate the whole phase move to in memory?
            }
        }

        var result = new SelectStatement(p.CommonTableExpressions, new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            p.SelectQuery.Projections,
            p.SelectQuery.From,
            p.SelectQuery.Where,
            boundGroup.Any() ? new GroupByClause(boundGroup) : null,
            p.SelectQuery.Having,
            p.SelectQuery.OrderBy,
            p.SelectQuery.UnionStatements));

        return new RemoteSqProviderSqlExecutionPlanNode(this, result, parent.Parameters);
    }

    public ExecutionPlanNode ProjectionExecutionPlanNode(ProjectionExecutionPlanNode node, RemoteSqProviderSqlExecutionPlanNode parent, ExecutionRewriteContext? context = null)
    {
        var p = parent.SqlStatement;

        // cehck to see if we are at a boundy point where we can't handle an expression, we need to split the projections at the projection, we could do partial projections and return an inmemory projection to fix up the rest!

        List<SelectProjection> boundProjections = new List<SelectProjection>();
        List<ProjectionColumnPlan> missedProjections = new List<ProjectionColumnPlan>();

        foreach (var col in node.Projections)
        {
            if (TryConvert(col.Value, context, out var expression))
            {
                var proj = new SelectProjection(expression, expression is IdentifierExpression e && e.Column == col.Alias ? null : col.Alias);
                if (!boundProjections.Contains(proj))
                {
                    boundProjections.Add(proj);
                }
            }
            else
            {
                context?.Report("UnsupportedExpressionForPushdown", $"Expression {col.Value} is not supported for pushdown");
                missedProjections.Add(col);
            }
        }

        if (!boundProjections.Any())
        {
            return node; // we can't push down any projections, so we need to return the node as is
        }

        if (missedProjections.Any())
        {
            return node; // we can't push down any projections, so we need to return the node as is
            throw new NotImplementedException("Need to create a new ProjectNode wrapping the remote sql node with passthru or missing projections included");
        }

        var result = new SelectStatement(p.CommonTableExpressions, new SelectExpression(
            false,
            p.SelectQuery.Top,
            p.SelectQuery.Distinctness,
            boundProjections,
            p.SelectQuery.From,
            p.SelectQuery.Where,
            p.SelectQuery.GroupBy,
            p.SelectQuery.Having,
            p.SelectQuery.OrderBy,
            p.SelectQuery.UnionStatements));

        return new RemoteSqProviderSqlExecutionPlanNode(this, result, parent.Parameters);
    }

    public bool TryConvert(ConditionExecutionPlan node, ExecutionRewriteContext? context, [NotNullWhen(true)] out Condition? condition)
    {
        condition = null;

        condition = node switch
        {
            LogicalConditionExecutionPlan logical => TryConvert(logical.Left, context, out var left) && TryConvert(logical.Right, context, out var right) ? new LogicalCondition(left, logical.Operator, right) : null,
            BinaryComparisonConditionExecutionPlan logical => TryConvert(logical.Left, context, out var left) && TryConvert(logical.Right, context, out var right) ? new BinaryComparisonCondition(left, logical.Operator, right) : null,
            BracketedConditionExecutionPlan bracketed => TryConvert(bracketed.Bracketed, context, out var con) ? new BracketedCondition (con) : null,
            UnaryConditionExecutionPlan unary => TryConvert(unary.Unary, context, out var con) ? new UnaryCondition(unary.Operator, con) : null,
            IsNullConditionExecutionPlan isNull => TryConvert(isNull.Expression, context, out var con) ? new IsNullCondition(isNull.IsNot, con) : null,
            LikeConditionExecutionPlan like => TryConvert(like.Left, context, out var left) && TryConvert(like.Right, context, out var right) ? new LikeCondition(like.IsNot, left, right) : null,
            BetweenConditionExecutionPlan between => TryConvert(between.Value, context, out var left) && TryConvert(between.From, context, out var from) && TryConvert(between.To, context, out var to) ? new BetweenCondition(between.IsNot, left, from, to) : null,
            InListConditionExecutionPlan inList => TryConvert(inList.Left, context, out var left) && TryConvertCollection(inList.Items, context, out var items) ? new InListCondition(inList.IsNot, left, items) : null,
            InSelectConditionExecutionPlan inSelect => TryConvertSelect(inSelect.ExecutionPlanNode, context, out var sel) && TryConvert(inSelect.Value, context, out var value) ?   new InSelectCondition(inSelect.IsNot, value, sel) : null,
            ExistsConditionExecutionPlan exists => TryConvertSelect(exists.ExecutionPlanNode, context, out var sel) ? new ExistsCondition(sel) : null,
            _ => throw new NotSupportedException($"Unsupported condition type: {node.GetType().Name}")
        };

        return condition != null;
    }


    public bool TryConvertSelect(ExecutionPlanNode node, ExecutionRewriteContext? context, [NotNullWhen(true)] out SelectExpression? expression)
    {
        if (node.Provider == this)
        {
            var plan = TryRewrite(node, context);

            if (plan is RemoteSqProviderSqlExecutionPlanNode remotePlan && remotePlan.Provider == this)
            {
                expression = new SelectExpression(true, remotePlan.SqlStatement.SelectQuery);
                return true;
            }
        }
        expression = null;
        return false;
    }
    public bool TryConvert(ExpressionPlanNode node, ExecutionRewriteContext? context, [NotNullWhen(true)] out Expression? expression)
    {
        expression = node switch
        {
            ScalarSubqueryExpressionExecutionPlan select => TryConvertSelect(select.Select, context, out var selExpression) ? selExpression : null,
            LiteralExpressionExecutionPlan nullLiteral when nullLiteral.Value is null => new NullValue(),
            LiteralExpressionExecutionPlan literal => new LiteralExpression(literal.Value),
            IdentifierExpressionExecutionPlan identifier => new IdentifierExpression(identifier.TableName, identifier.ColumnName)
            {
                Binding = identifier.ColumnBinding
            },
            StartExpressionExecutionPlan star => new StarIdentifierExpression()
            {
                Bindings = star.Bindings
            }, // count(1) ?? 
            ParameterExpressionExecutionPlan parameter => new ParameterIdentifier(parameter.ParameterName)
            {
                Binding = new ParameterBinding(parameter.Parameter!)
            }, // need to extract the paramater value from this too i think!
            BracketedExpressionExecutionPlan bracketed => TryConvert(bracketed.Expression, context, out var be) ? new BracketedExpression(be) : null,

            SubValueInExpressionExecutionPlan inExpression => TryConvert(inExpression.Value, context, out var valExp) && TryConvert(inExpression.SubValue, context, out var subValExp) ? new InExpression(valExp, subValExp) : null,
            ConvertExpressionExecutionPlan asExpression => TryConvert(asExpression.Value, context, out var exp) ? new AsExpression(exp, asExpression.DataType) : null,
            ValuePartExpressionPlanNode valueFromExpression => TryConvert(valueFromExpression.Value, context, out var valExp) ? new ValueFromExpression(valueFromExpression.Part, valExp) : null,
            BinaryOperatorExpressionExecutionPlan binary => TryConvert(binary.Left, context, out var l) && TryConvert(binary.Right, context, out var r) ? new BinaryOperatorExpression(l, binary.Operator, r) : null,
            UnaryOperatorExpressionExecutionPlan unary => TryConvert(unary.Inner, context, out var l) ? new UnaryOperatorExpression(l, unary.Operator) : null,
            // CaseExpressionExecutionPlan caseExpression => new CaseExpression(caseExpression),
            DistinctValueExpressionExecutionPlan distinctValue => TryConvert(distinctValue.Expression, context, out var valExp) ? new DistinctValueExpression(distinctValue.Distinctness, valExp) : null,
            CurrentDateExpressionExecutionPlan => new CurrentDate(),
            CurrentTimeExpressionExecutionPlan => new CurrentTime(),
            CurrentTimestampExpressionExecutionPlan => new CurrentTimestamp(),


            AggregateFunctionCallExpressionExecutionPlan agg => IsFunctionSupportedForPushdown(agg.Expression) && TryConvertCollection(agg.Arguments, context, out var expressions) ? new FunctionCallExpression(agg.Expression.FunctionName, expressions, agg.Expression.Window)
            {
                Binding = agg.Expression.Binding
            } : null,
            ValueFunctionCallExpressionExecutionPlan agg => IsFunctionSupportedForPushdown(agg.Expression) && TryConvertCollection(agg.Arguments, context, out var expressions) ? new FunctionCallExpression(agg.Expression.FunctionName, expressions, agg.Expression.Window)
            {
                Binding = agg.Expression.Binding
            } : null,

            _ => throw new NotSupportedException($"Unsupported expression type: {node.GetType().Name}")
        };

        if (expression is null && node is AggregateFunctionCallExpressionExecutionPlan p)
        {
            context?.Report("UnsupportedFunctionForPushdown", $"Function {p.Expression.FunctionName} is not supported for pushdown");
        }

        if (expression is null && node is ValueFunctionCallExpressionExecutionPlan v)
        {
            context?.Report("UnsupportedFunctionForPushdown", $"Function {v.Expression.FunctionName} is not supported for pushdown");
        }

        return expression is not null;
    }

    public bool TryConvertCollection(IEnumerable<ExpressionPlanNode> nodes, ExecutionRewriteContext? context, [NotNullWhen(true)] out IEnumerable<Expression>? expressions)
    {
        var res = new List<Expression>();
        foreach (var node in nodes)
        {
            if (TryConvert(node, context, out var expression))
            {
                res.Add(expression);
            }
            else
            {
                expressions = null;
                return false;
            }
        }
        expressions = res;
        return true;
    }


    protected bool IsFunctionSupportedForPushdown(FunctionCallExpression functionCall)
    {
        var rewrittenCall = RewriteFunctionCallForPushdown(functionCall);
        var function = rewrittenCall.Binding is not null && rewrittenCall.Binding.Name.Equals(rewrittenCall.FunctionName, StringComparison.OrdinalIgnoreCase)
            ? rewrittenCall.Binding
            : new FunctionSymbol(rewrittenCall.FunctionName, rewrittenCall.Binding?.IsAggregate ?? false, rewrittenCall.Binding?.Arguments ?? Array.Empty<FunctionArgumentSymbol>(), rewrittenCall.Binding?.ParamsArgument);
        return SupportsFunction(function);
    }
    protected abstract FunctionCallExpression RewriteFunctionCallForPushdown(FunctionCallExpression functionCall);

    protected abstract bool SupportsFunction(FunctionSymbol function);

    public abstract Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);
}

public class RemoteSqProviderSqlExecutionPlanNode : ExecutionPlanNode
{
    private readonly RemoteSqlProviderBase _remoteSqlProviderBase;
    private readonly SelectStatement _select;
    private readonly IEnumerable<ExecuterParameter>? _parameters;

    public bool TableScan { get; init; }

    public string? Alias { get; init; }

    public SelectStatement SqlStatement => _select;

    public IEnumerable<ExecuterParameter>? Parameters => _parameters;

    public RemoteSqProviderSqlExecutionPlanNode(
        RemoteSqlProviderBase remoteSqlProviderBase,
        SelectStatement select,
        IEnumerable<ExecuterParameter>? parameters)
    {
        _remoteSqlProviderBase = remoteSqlProviderBase;
        _select = select;
        _parameters = parameters;
    }

    public override IExecutionProvider? Provider => _remoteSqlProviderBase;

    public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => _remoteSqlProviderBase.ExecuteAsync(_select, parameters, cancellationToken);

    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null) => null;
}