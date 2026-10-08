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

    private bool TryGetParentQuery(ExecutionPlanNode node, [NotNullWhen(true)] out RemoteSqProviderSqlExecutionPlanNode? res)
    {
        if (node.Provider == this && node is RemoteSqProviderSqlExecutionPlanNode n)
        {
            res = n;
            return true;
        }

        if (node is SubqueryExecutionPlanNode subquery && TryGetParentQuery(subquery.Inner, out res))
        {
            return true;
        }

        if (node is FilterExecutionPlanNode filter && TryGetParentQuery(filter.Input, out res))
        {
            return true;
        }

        if (node is JoinExecutionPlanNode join)
        {
            if (TryGetParentQuery(join.Left, out res))
            {
                return true;
            }

            if (TryGetParentQuery(join.Right, out res))
            {
                return true;
            }
        }

        res = null;
        return false;
    }

    private static IReadOnlyList<CommonTableExpression> CollectCteDefinitions(SyntaxNode node, HashSet<string>? seen = null)
    {
        seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ctes = new List<CommonTableExpression>();

        void Visit(SyntaxNode? current)
        {
            if (current is null)
            {
                return;
            }

            if (current is NamedTableReference namedTable && namedTable.Binding is not null)
            {
                if (namedTable.Binding.GetState<CteTableMetadata>() is CteTableMetadata cte && seen.Add(cte.Alias))
                {
                    ctes.Add(new CommonTableExpression(cte.Alias, cte.Body));
                    Visit(cte.Body);
                }
            }

            foreach (var child in current.GetChildren())
            {
                Visit(child);
            }
        }

        Visit(node);
        return ctes;
    }

    private static IEnumerable<CommonTableExpression> CollectCteDefinitionsFromPlan(ExecutionPlanNode node, HashSet<string>? seen = null)
    {
        seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var collected = new List<(string Alias, SelectExpression Body, int Order)>();

        void Visit(ExecutionPlanNode current)
        {
            if (current is RemoteSqProviderSqlExecutionPlanNode remoteSql)
            {
                foreach (var cte in remoteSql.Sql.CommonTableExpressions)
                {
                    if (seen.Add(cte.Alias))
                    {
                        collected.Add((cte.Alias, cte.Body, int.MaxValue));
                    }
                }
            }

            if (current is SubqueryExecutionPlanNode subquery)
            {
                if (subquery.IsCte && !string.IsNullOrEmpty(subquery.CteName) && subquery.CteBody is not null)
                {
                    if (seen.Add(subquery.CteName))
                    {
                        collected.Add((subquery.CteName, subquery.CteBody, subquery.DeclarationOrder ?? int.MaxValue));
                    }
                }

                Visit(subquery.Inner);
            }

            if (current is TableScanExecutionPlanNode tableScan && tableScan.Table.GetState<CteTableMetadata>() is CteTableMetadata cteMetadata)
            {
                if (seen.Add(cteMetadata.Alias))
                {
                    collected.Add((cteMetadata.Alias, cteMetadata.Body, cteMetadata.DeclarationOrder));
                }
            }

            foreach (var child in current.GetInputs())
            {
                Visit(child);
            }
        }

        Visit(node);
        foreach (var cte in collected.OrderBy(x => x.Order).ThenBy(x => x.Alias, StringComparer.OrdinalIgnoreCase))
        {
            yield return new CommonTableExpression(cte.Alias, cte.Body);
        }
    }

    private static IEnumerable<CommonTableExpression> MergeCteDefinitions(params IEnumerable<CommonTableExpression>[] cteSets)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cte in cteSets.SelectMany(set => set))
        {
            if (seen.Add(cte.Alias))
            {
                yield return cte;
            }
        }
    }

    private static SyntaxNode ResolveCteSource(SubqueryExecutionPlanNode subquery, SyntaxNode fallback)
        => subquery.CteBody ?? fallback;

    public ExecutionPlanNode TryRewrite(ExecutionPlanNode node, ExecutionRewriteContext? context = null)
        => RewriteUnion(node, context) ??
            RewriteJoinsBothSideSql(node, context) ??
            RewriteFilter(node, context) ??
            RewriteProjection(node, context) ??
            RewriteTableScan(node, context)
            ?? node;

    private ExecutionPlanNode? RewriteUnion(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not UnionExecutionPlanNode union)
        {
            return null;
        }

        var branchQueries = new List<(SelectStatement Sql, bool IsAll)>();
        foreach (var operation in union.Operations)
        {
            if (!TryGetParentQuery(operation.Input, out var parentQuery))
            {
                return null;
            }

            branchQueries.Add((parentQuery.Sql, operation.IsAll));
        }

        if (branchQueries.Count == 0)
        {
            return null;
        }

        var first = branchQueries[0].Sql;
        var unionStatements = new List<UnionStatement>();
        foreach (var (sql, isAll) in branchQueries.Skip(1))
        {
            unionStatements.Add(new UnionStatement(isAll, sql.SelectQuery));
        }

        var mergedCtes = new List<CommonTableExpression>();
        var seenCtes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var operation in union.Operations)
        {
            if (TryGetParentQuery(operation.Input, out var parentQuery))
            {
                foreach (var cte in parentQuery.Sql.CommonTableExpressions)
                {
                    if (seenCtes.Add(cte.Alias))
                    {
                        mergedCtes.Add(cte);
                    }
                }

                foreach (var cte in CollectCteDefinitions(parentQuery.Sql.SelectQuery, seenCtes))
                {
                    if (seenCtes.Add(cte.Alias))
                    {
                        mergedCtes.Add(cte);
                    }
                }
            }

            foreach (var cte in CollectCteDefinitionsFromPlan(operation.Input, seenCtes))
            {
                if (seenCtes.Add(cte.Alias))
                {
                    mergedCtes.Add(cte);
                }
            }
        }

        mergedCtes = mergedCtes
            .DistinctBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var select = new SelectExpression(
            first.SelectQuery.IsBracketed,
            first.SelectQuery.Top,
            first.SelectQuery.Distinctness,
            first.SelectQuery.Projections,
            first.SelectQuery.From,
            first.SelectQuery.Where,
            first.SelectQuery.GroupBy,
            first.SelectQuery.Having,
            first.SelectQuery.OrderBy,
            unionStatements);

        var result = new SelectStatement(mergedCtes.DistinctBy(x => x.Alias, StringComparer.OrdinalIgnoreCase), select);
        context?.Report("SqlPushdownApplied", "Union inputs were merged into a single SQL query.", ExecutionRewriteDiagnosticLevel.Debug, nameof(UnionExecutionPlanNode), union.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, result);
    }

    private ExecutionPlanNode? RewriteTableScan(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not TableScanExecutionPlanNode tableScan)
        {
            return null;
        }
        var tables = GetTables();
        var table = tables.FirstOrDefault(x => x.TableName == tableScan.TableName && x.SchemaName == tableScan.SchemaName)
            ?? tables.FirstOrDefault(x => x.TableName == tableScan.TableName);

        if (table is null)
        {
            return null;
        }

        IEnumerable<ColumnSymbol> columns = tableScan.ReferencedColumns is { Count: > 0 }
            ? tableScan.ReferencedColumns
            : tableScan.Columns;

        var tableAlias = string.Equals(tableScan.Alias, tableScan.TableName, StringComparison.OrdinalIgnoreCase) ? null : tableScan.Alias;
        var tableReference = new NamedTableReference(IdentifierExpression.FromTableSymbol(tableScan.Table), tableAlias)
        {
            Binding = tableScan.Table,
        };
        var sql = new SelectExpression(
            false,
            null,
            Distinctness.Unspecified,
            columns.Select(x => new SelectProjection(new IdentifierExpression(tableScan.Alias, x.Name))),
            new FromClause([tableReference], []),
            null,
            null,
            null,
            null,
            []);

        return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], sql));
    }
    private ExecutionPlanNode? RewriteFilter(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not FilterExecutionPlanNode filter || !TryGetParentQuery(filter.Input, out var parentQuery))
        {
            return null;
        }

        var ctes = parentQuery.Sql.CommonTableExpressions;
        var sourceSelect = parentQuery.Sql.SelectQuery;
        if (filter.Input is SubqueryExecutionPlanNode filterSubquery)
        {
            var seen = new HashSet<string>(parentQuery.Sql.CommonTableExpressions.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase);
            var nestedCtes = CollectCteDefinitions(ResolveCteSource(filterSubquery, parentQuery.Sql.SelectQuery), seen);
            var currentCte = filterSubquery.IsCte && !string.IsNullOrEmpty(filterSubquery.CteName) && filterSubquery.CteBody is not null
                ? [new CommonTableExpression(filterSubquery.CteName, filterSubquery.CteBody)]
                : Enumerable.Empty<CommonTableExpression>();
            ctes = MergeCteDefinitions(parentQuery.Sql.CommonTableExpressions, nestedCtes, currentCte).ToList();

            var (subqueryProjections, subqueryGroupBys, subqueryOrderBys, subqueryTop, subqueryOffset, subqueryDistinctness) = GetProjectionState(filterSubquery.Inner);
            var subquerySelect = filterSubquery.Inner is ProjectionExecutionPlanNode projection
                ? BuildSelectFromProjection(projection)
                : BuildSelectFromPlan(filterSubquery.Inner, subqueryProjections, subqueryGroupBys, subqueryOrderBys, subqueryTop, subqueryOffset, subqueryDistinctness);
            sourceSelect = new SelectExpression(
                sourceSelect.IsBracketed,
                sourceSelect.Top,
                sourceSelect.Distinctness,
                sourceSelect.Projections,
                new FromClause([new SelectTableReference(new SelectExpression(true, subquerySelect), filterSubquery.Alias ?? "subquery")], []),
                null,
                sourceSelect.GroupBy,
                sourceSelect.Having,
                sourceSelect.OrderBy,
                sourceSelect.UnionStatements);
        }

        var nestedExpressionCtes = CollectCteDefinitions(filter.Condition, new HashSet<string>(ctes.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase));
        ctes = MergeCteDefinitions(ctes, nestedExpressionCtes).ToList();

        if (ContainsUnsupportedFunction(filter.Condition))
        {
            context?.Report("SqlPushdownSkipped", "Filter could not be pushed to SQL because it contains an unsupported function.", ExecutionRewriteDiagnosticLevel.Warning, nameof(FilterExecutionPlanNode), filter.Condition.ToString());
            return null;
        }

        var where = sourceSelect.Where is null
            ? new WhereClause(filter.Condition)
            : new WhereClause(new LogicalCondition(sourceSelect.Where.Condition, LogicalOperator.And, filter.Condition));

        var sql = new SelectExpression(
            sourceSelect.IsBracketed,
            sourceSelect.Top,
            sourceSelect.Distinctness,
            sourceSelect.Projections,
            sourceSelect.From,
            where,
            sourceSelect.GroupBy,
            sourceSelect.Having,
            sourceSelect.OrderBy,
            sourceSelect.UnionStatements);

        var select = new SelectStatement(ctes.Distinct(), sql);
        return new RemoteSqProviderSqlExecutionPlanNode(this, select);
    }

    private ExecutionPlanNode? RewriteProjection(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        if (node is not ProjectionExecutionPlanNode projection || projection.Input.Provider != this)
        {
            return null;
        }

        var sourceSelect = BuildSelectFromProjection(projection);
        var unsupportedFunctions = GetUnsupportedFunctionCalls(sourceSelect).ToList();
        if (unsupportedFunctions.Count > 0)
        {
            foreach (var unsupportedFunction in unsupportedFunctions)
            {
                context?.Report(
                    unsupportedFunction.Binding is not null ? $"SqlPushdownUnsupportedFunction:{unsupportedFunction.FunctionName}" : "SqlPushdownUnsupportedFunction",
                    $"Projection depends on unsupported function '{unsupportedFunction.FunctionName}' and cannot be fully pushed to SQL.",
                    ExecutionRewriteDiagnosticLevel.Warning,
                    nameof(ProjectionExecutionPlanNode),
                    unsupportedFunction.ToString());
            }

            var pushedDownSelect = TryCreatePushdownSelect(sourceSelect);
            if (pushedDownSelect is not null)
            {
                context?.Report(
                    "SqlPushdownPartial",
                    "Projection partially pushed to SQL; unsupported function(s) will be evaluated in memory.",
                    ExecutionRewriteDiagnosticLevel.Info,
                    nameof(ProjectionExecutionPlanNode),
                    sourceSelect.ToString());
                var pushedDownInput = new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement([], pushedDownSelect));

                return new ProjectionExecutionPlanNode(pushedDownInput, projection.Projections, projection.GroupBys, projection.OrderBys, projection.Top, projection.Offset, projection.Distinctness);
            }

            context?.Report(
                "SqlPushdownSkipped",
                "Projection could not be pushed to SQL because it depends on unsupported function(s).",
                ExecutionRewriteDiagnosticLevel.Warning,
                nameof(ProjectionExecutionPlanNode),
                sourceSelect.ToString());
            return null;
        }

        var ctes = CollectCteDefinitionsFromPlan(projection.Input).ToList();
        context?.Report("SqlPushdownApplied", "Projection was pushed to SQL provider.", ExecutionRewriteDiagnosticLevel.Debug, nameof(ProjectionExecutionPlanNode), sourceSelect.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, new SelectStatement(ctes, sourceSelect));
    }

    private static SelectExpression BuildSelectFromProjection(ProjectionExecutionPlanNode projection)
        => BuildSelectFromPlan(projection.Input, projection.Projections, projection.GroupBys, projection.OrderBys, projection.Top, projection.Offset, projection.Distinctness);

    private static (IReadOnlyList<ProjectionColumnPlan> Projections, IReadOnlyList<ExpressionPlanNode> GroupBys, IReadOnlyList<OrderByExpressionPlan> OrderBys, long? Top, OffsetExpressionPlan? Offset, Distinctness Distinctness) GetProjectionState(ExecutionPlanNode node)
        => node switch
        {
            ProjectionExecutionPlanNode projection => (projection.Projections, projection.GroupBys, projection.OrderBys, projection.Top, projection.Offset, projection.Distinctness),
            FilterExecutionPlanNode filter => GetProjectionState(filter.Input),
            SubqueryExecutionPlanNode subquery => GetProjectionState(subquery.Inner),
            _ => (Array.Empty<ProjectionColumnPlan>(), Array.Empty<ExpressionPlanNode>(), Array.Empty<OrderByExpressionPlan>(), null, null, Distinctness.Unspecified)
        };

    private static TableReference BuildTableReference(TableScanExecutionPlanNode tableScan)
    {
        var alias = string.Equals(tableScan.Alias, tableScan.TableName, StringComparison.OrdinalIgnoreCase) ? null : tableScan.Alias;
        var identifier = IdentifierExpression.FromTableSymbol(tableScan.Table);
        return new NamedTableReference(identifier, alias)
        {
            Binding = tableScan.Table,
        };
    }

    private static SelectExpression BuildSelectFromPlan(ExecutionPlanNode node, IReadOnlyList<ProjectionColumnPlan> projections, IReadOnlyList<ExpressionPlanNode> groupBys, IReadOnlyList<OrderByExpressionPlan> orderBys, long? top = null, OffsetExpressionPlan? offsetPlan = null, Distinctness distinctness = Distinctness.Unspecified)
    {
        static bool NeedsDerivedTableWrapper(ExecutionPlanNode source)
            => source is SubqueryExecutionPlanNode
                || source is ProjectionExecutionPlanNode projection &&
                    (
                        projection.Top.HasValue
                        || projection.OrderBys.Count > 0
                        || projection.Offset is not null
                        || projection.Input is SubqueryExecutionPlanNode
                    )
                || source is FilterExecutionPlanNode filter && NeedsDerivedTableWrapper(filter.Input);

        var selectProjections = projections.Count == 0
            ? [new SelectProjection(new StarIdentifierExpression())]
            : projections.Select(CreateSelectProjection);

        var groupByClause = groupBys.Count == 0 ? null : new GroupByClause(groupBys.Select(ToSqlExpression));
        var offsetClause = ToOffsetClause(offsetPlan);
        var orderByClause = orderBys.Count == 0
            ? offsetClause is null ? null : new OrderByClause([new OrderByEntry(new LiteralExpression(1), OrderByDirection.Unspecified)], offsetClause)
            : new OrderByClause(orderBys.Select(x => new OrderByEntry(ToSqlExpression(x.Value), x.Direction)), offsetClause);

        return node switch
        {
            FilterExecutionPlanNode filter => MergeWhere(BuildSelectFromPlan(filter.Input, projections, groupBys, orderBys, top, offsetPlan, distinctness), filter.Condition),
            JoinExecutionPlanNode join when join.JoinType == JoinType.Cross && join.Condition is null =>
                NeedsDerivedTableWrapper(join.Left) || NeedsDerivedTableWrapper(join.Right)
                    ? new SelectExpression(
                        false,
                        top,
                        distinctness,
                        selectProjections,
                        new FromClause(
                            [ToTableReference(join.Left)],
                            [new JoinClause(join.JoinSide, join.JoinType, ToTableReference(join.Right), null)]),
                        null,
                        groupByClause,
                        null,
                        orderByClause,
                        [])
                    : new SelectExpression(
                        false,
                        top,
                        distinctness,
                        selectProjections,
                        new FromClause([ToTableReference(join.Left), ToTableReference(join.Right)], []),
                        null,
                        groupByClause,
                        null,
                        orderByClause,
                        []),
            JoinExecutionPlanNode join => new SelectExpression(
                false,
                top,
                distinctness,
                selectProjections,
                new FromClause([ToTableReference(join.Left)], [new JoinClause(join.JoinSide, join.JoinType, ToTableReference(join.Right), join.Condition)]),
                null,
                groupByClause,
                null,
                orderByClause,
                []),
            TableScanExecutionPlanNode tableScan => new SelectExpression(
                false,
                top,
                distinctness,
                selectProjections,
                new FromClause([BuildTableReference(tableScan)], []),
                null,
                groupByClause,
                null,
                orderByClause,
                []),
            SubqueryExecutionPlanNode subquery when subquery.IsCte => new SelectExpression(
                false,
                top,
                distinctness,
                selectProjections,
                new FromClause([new NamedTableReference(new IdentifierExpression(subquery.CteName!), string.Equals(subquery.Alias, subquery.CteName, StringComparison.OrdinalIgnoreCase) ? null : subquery.Alias)], []),
                null,
                groupByClause,
                null,
                orderByClause,
                []),
            SubqueryExecutionPlanNode subquery =>
                new SelectExpression(
                    false,
                    top,
                    distinctness,
                    selectProjections,
                    new FromClause([
                        new SelectTableReference(
                            new SelectExpression(
                                true,
                                subquery.Inner is ProjectionExecutionPlanNode projection
                                    ? BuildSelectFromProjection(projection)
                                    : BuildSelectFromPlan(subquery.Inner, GetProjectionState(subquery.Inner).Projections, GetProjectionState(subquery.Inner).GroupBys, GetProjectionState(subquery.Inner).OrderBys, GetProjectionState(subquery.Inner).Top, GetProjectionState(subquery.Inner).Offset, GetProjectionState(subquery.Inner).Distinctness)),
                            subquery.Alias ?? "subquery")
                    ], []),
                    null,
                    groupByClause,
                    null,
                    orderByClause,
                    []),
            RemoteSqProviderSqlExecutionPlanNode remote => BuildSelectFromRemote(remote, projections, groupBys, orderBys, top, offsetPlan, distinctness),
            _ => new SelectExpression(
                false,
                top,
                distinctness,
                selectProjections,
                new FromClause([], []),
                null,
                groupByClause,
                null,
                orderByClause,
                [])
        };
    }

    private static SelectExpression BuildSelectFromRemote(RemoteSqProviderSqlExecutionPlanNode remote, IReadOnlyList<ProjectionColumnPlan> projections, IReadOnlyList<ExpressionPlanNode> groupBys, IReadOnlyList<OrderByExpressionPlan> orderBys, long? top = null, OffsetExpressionPlan? offsetPlan = null, Distinctness distinctness = Distinctness.Unspecified)
    {
        var select = remote.Sql.SelectQuery;
        var selectProjections = projections.Count == 0
            ? select.Projections
            : projections.Select(CreateSelectProjection);

        var groupByClause = groupBys.Count == 0 ? select.GroupBy : new GroupByClause(groupBys.Select(ToSqlExpression));
        var effectiveOffset = ToOffsetClause(offsetPlan) ?? select.OrderBy?.OffsetClause;
        var orderByClause = orderBys.Count > 0
            ? new OrderByClause(orderBys.Select(x => new OrderByEntry(ToSqlExpression(x.Value), x.Direction)), effectiveOffset)
            : select.OrderBy is not null
                ? new OrderByClause(select.OrderBy.Entries, effectiveOffset ?? select.OrderBy.OffsetClause)
                : effectiveOffset is null
                    ? null
                    : new OrderByClause([], effectiveOffset);

        return new SelectExpression(
            select.IsBracketed,
            top ?? select.Top,
            distinctness == Distinctness.Unspecified ? select.Distinctness : distinctness,
            selectProjections,
            select.From,
            select.Where,
            groupByClause,
            select.Having,
            orderByClause,
            select.UnionStatements);
    }

    private static OffsetClause? ToOffsetClause(OffsetExpressionPlan? offsetPlan)
    {
        if (offsetPlan is null)
        {
            return null;
        }

        var offsetCount = offsetPlan.OffsetCount is null ? null : ToSqlExpression(offsetPlan.OffsetCount);
        var fetchCount = offsetPlan.FetchCount is null ? null : ToSqlExpression(offsetPlan.FetchCount);
        return offsetCount is null && fetchCount is null ? null : new OffsetClause(offsetCount, fetchCount);
    }

    private static SelectExpression MergeWhere(SelectExpression select, Condition? condition)
    {
        if (condition is null)
        {
            return select;
        }

        var where = select.Where is null
            ? new WhereClause(condition)
            : new WhereClause(new LogicalCondition(select.Where.Condition, LogicalOperator.And, condition));

        return new SelectExpression(
            select.IsBracketed,
            select.Top,
            select.Distinctness,
            select.Projections,
            select.From,
            where,
            select.GroupBy,
            select.Having,
            select.OrderBy,
            select.UnionStatements);
    }

    private static TableReference ToTableReference(ExecutionPlanNode node)
    {
        if (node is FilterExecutionPlanNode filter)
        {
            var (sourceProjections, sourceGroupBys, sourceOrderBys, sourceTop, sourceOffset, sourceDistinctness) = GetProjectionState(filter.Input);
            var source = BuildSelectFromPlan(filter.Input, sourceProjections, sourceGroupBys, sourceOrderBys, sourceTop, sourceOffset, sourceDistinctness);
            return new SelectTableReference(new SelectExpression(
                source.IsBracketed,
                source.Top,
                source.Distinctness,
                source.Projections,
                source.From,
                filter.Condition is null ? source.Where : new WhereClause(filter.Condition),
                source.GroupBy,
                source.Having,
                source.OrderBy,
                source.UnionStatements), "subquery");
        }

        if (node is SubqueryExecutionPlanNode subquery)
        {
            var (subqueryProjections, subqueryGroupBys, subqueryOrderBys, subqueryTop, subqueryOffset, subqueryDistinctness) = GetProjectionState(subquery.Inner);
            return new SelectTableReference(
                new SelectExpression(
                    true,
                    subquery.Inner is ProjectionExecutionPlanNode plan
                        ? BuildSelectFromPlan(plan.Input, plan.Projections, plan.GroupBys, plan.OrderBys, plan.Top, plan.Offset, plan.Distinctness)
                        : BuildSelectFromPlan(subquery.Inner, subqueryProjections, subqueryGroupBys, subqueryOrderBys, subqueryTop, subqueryOffset, subqueryDistinctness)),
                subquery.Alias ?? "subquery");
        }

        return node switch
        {
            TableScanExecutionPlanNode tableScan => BuildTableReference(tableScan),
            ProjectionExecutionPlanNode projection => new SelectTableReference(new SelectExpression(true, BuildSelectFromProjection(projection)), "subquery"),
            RemoteSqProviderSqlExecutionPlanNode remote => new SelectTableReference(new SelectExpression(true, remote.Sql.SelectQuery), "subquery"),
            _ => throw new NotSupportedException($"Unsupported table source: {node.GetType().Name}")
        };
    }

    private static SelectProjection CreateSelectProjection(ProjectionColumnPlan projection)
    {
        var expression = ToSqlExpression(projection.Value);
        var alias = projection.Alias;
        if (alias == "*" || alias == null)
        {
            return new SelectProjection(new StarIdentifierExpression());
        }

        var isSynthetic = false;
        if (string.Equals(alias, expression.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            alias = null;
            isSynthetic = true;
        }
        else if (expression is IdentifierExpression identifier)
        {
            var bareColumn = identifier.Column;
            var normalizedAlias = alias.Trim('[', ']');
            if (string.Equals(normalizedAlias, bareColumn, StringComparison.OrdinalIgnoreCase))
            {
                alias = null;
                isSynthetic = true;
            }
        }

        return new SelectProjection(expression, alias, isSynthetic);
    }

    private static Expression ToSqlExpression(ExpressionPlanNode expression)
        => expression switch
        {
            IdentifierExpressionExecutionPlan identifier => identifier.Identifier,
            LiteralExpressionExecutionPlan literal => new LiteralExpression(literal.Value),
            ParameterExpressionExecutionPlan parameter => new ParameterIdentifier(parameter.Parameter.ParamterName),
            CurrentDateExpressionExecutionPlan => new CurrentDate(),
            CurrentTimeExpressionExecutionPlan => new CurrentTime(),
            CurrentTimestampExpressionExecutionPlan => new CurrentTimestamp(),
            ConstantExpressionExecutionPlan constant =>
                constant.Execute(new RowAccessor(Array.Empty<string>(), Array.Empty<object?>())) is null
                    ? new NullValue()
                    : new LiteralExpression(constant.Execute(new RowAccessor(Array.Empty<string>(), Array.Empty<object?>()))),
            BinaryOperatorExpressionExecutionPlan binary => new BinaryOperatorExpression(ToSqlExpression(binary.Left), binary.Operator, ToSqlExpression(binary.Right)),
            UnaryOperatorExpressionExecutionPlan unary => new UnaryOperatorExpression(ToSqlExpression(unary.Inner), unary.Operator),
            ValueFunctionCallExpressionExecutionPlan valueFunction => valueFunction.Expression,
            AggregateFunctionCallExpressionExecutionPlan aggregateFunction => aggregateFunction.Expression,
            CaseExpressionExecutionPlan caseExpression => caseExpression.Expression,
            ValuePartExpressionPlanNode valuePart => new ValueFromExpression(valuePart.Part, ToSqlExpression(valuePart.Value)),
            SubValueInExpressionExecutionPlan inExpression => new InExpression(ToSqlExpression(inExpression.SubValue), ToSqlExpression(inExpression.Value)),
            ConvertExpressionExecutionPlan convert => new AsExpression(ToSqlExpression(convert.Value), convert.DataType),
            ScalarSubqueryExpressionExecutionPlan scalarSubquery => scalarSubquery.Select,
            _ => throw new NotSupportedException($"Unsupported expression plan type: {expression.GetType().Name}")
        };

    private static Condition ToSqlCondition(ConditionExecutionPlan condition)
        => condition switch
        {
            BinaryComparisonConditionExecutionPlan comparison => new BinaryComparisonCondition(
                ToSqlExpression(comparison.Left),
                comparison.Operator,
                ToSqlExpression(comparison.Right)),
            LogicalConditionExecutionPlan logical => new LogicalCondition(
                ToSqlCondition(logical.Left),
                logical.Operator,
                ToSqlCondition(logical.Right)),
            BracketedConditionExecutionPlan bracketed => new BracketedCondition(ToSqlCondition(bracketed.Bracketed)),
            UnaryConditionExecutionPlan unary => new UnaryCondition(unary.Operator, ToSqlCondition(unary.Unary)),
            IsNullConditionExecutionPlan isNull => new IsNullCondition(isNull.IsNot, ToSqlExpression(isNull.Expression)),
            LikeConditionExecutionPlan like => new LikeCondition(like.IsNot, ToSqlExpression(like.Left), ToSqlExpression(like.Right)),
            BetweenConditionExecutionPlan between => new BetweenCondition(between.IsNot, ToSqlExpression(between.Value), ToSqlExpression(between.From), ToSqlExpression(between.To)),
            InListConditionExecutionPlan inList => new InListCondition(inList.IsNot, ToSqlExpression(inList.Left), inList.Items.Select(ToSqlExpression)),
            _ => throw new NotSupportedException($"Unsupported condition plan: {condition.GetType().Name}")
        };

    private SelectExpression? TryCreatePushdownSelect(SelectExpression source)
    {
        var projections = new List<SelectProjection>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var projection in source.Projections)
        {
            if (ContainsUnsupportedFunction(projection.Expression))
            {
                foreach (var dependent in CollectPushdownDependencies(projection.Expression))
                {
                    var key = dependent.Expression.ToString();
                    if (seen.Add(key))
                    {
                        projections.Add(dependent);
                    }
                }

                continue;
            }

            var key2 = projection.Expression.ToString();
            if (seen.Add(key2))
            {
                projections.Add(projection);
            }
        }

        if (projections.Count == 0)
        {
            return null;
        }

        var where = source.Where;
        var groupBy = source.GroupBy;
        var having = source.Having;
        var orderBy = source.OrderBy;

       return new SelectExpression(
            source.IsBracketed,
            source.Top,
            source.Distinctness,
            projections,
            source.From,
            where,
            groupBy,
            having,
            orderBy,
            source.UnionStatements);
    }

    private IEnumerable<SelectProjection> CollectPushdownDependencies(Expression expression)
    {
        if (expression is FunctionCallExpression functionCall && functionCall.Binding is not null && !IsFunctionSupportedForPushdown(functionCall))
        {
            foreach (var argument in functionCall.Arguments)
            {
                foreach (var dependency in CollectPushdownDependencies(argument))
                {
                    yield return dependency;
                }
            }

            yield break;
        }

        foreach (var child in expression.GetChildren())
        {
            if (child is Expression childExpression)
            {
                foreach (var dependency in CollectPushdownDependencies(childExpression))
                {
                    yield return dependency;
                }
            }
        }

        if (expression is IdentifierExpression or LiteralExpression or ParameterIdentifier or ValueFromExpression)
        {
            yield return new SelectProjection(expression);
        }
    }

    private bool ContainsUnsupportedFunction(SyntaxNode node)
        => GetUnsupportedFunctionCalls(node).Any();

    protected virtual FunctionCallExpression RewriteFunctionCallForPushdown(FunctionCallExpression functionCall)
        => functionCall;

    protected bool IsFunctionSupportedForPushdown(FunctionCallExpression functionCall)
    {
        var rewrittenCall = RewriteFunctionCallForPushdown(functionCall);
        var function = rewrittenCall.Binding is not null && rewrittenCall.Binding.Name.Equals(rewrittenCall.FunctionName, StringComparison.OrdinalIgnoreCase)
            ? rewrittenCall.Binding
            : new FunctionSymbol(rewrittenCall.FunctionName, rewrittenCall.Binding?.IsAggregate ?? false, rewrittenCall.Binding?.Arguments ?? Array.Empty<FunctionArgumentSymbol>(), rewrittenCall.Binding?.ParamsArgument);
        return SupportsFunction(function);
    }

    private IEnumerable<FunctionCallExpression> GetUnsupportedFunctionCalls(SyntaxNode node)
    {
        if (node is FunctionCallExpression functionCall && functionCall.Binding is not null && !IsFunctionSupportedForPushdown(functionCall))
        {
            yield return functionCall;
        }

        foreach (var child in node.GetChildren())
        {
            foreach (var unsupported in GetUnsupportedFunctionCalls(child))
            {
                yield return unsupported;
            }
        }
    }

    protected virtual bool SupportsFunction(FunctionSymbol function)
    {
        if (function.GetState<SqlFunction>() is not SqlFunction sqlFunction)
        {
            return true;
        }

        return sqlFunction.Name.Equals("COUNT", StringComparison.OrdinalIgnoreCase);
    }

    private ExecutionPlanNode? RewriteJoinsBothSideSql(ExecutionPlanNode node, ExecutionRewriteContext? context)
    {
        // we might be able to special case we one side is not sql and convert it to the `select foo in () pattern`
        if (node is not JoinExecutionPlanNode join)
        {
            return null;
        }

        if (join.Left is JoinExecutionPlanNode || join.Right is JoinExecutionPlanNode)
        {
            return null;
        }

        if (!TryGetParentQuery(join.Left, out var left) || !TryGetParentQuery(join.Right, out var right))
        {
            return null;
        }

        if (left.Provider != this || right.Provider != this)
        {
            return null;
        }

        if (left.Sql.SelectQuery.From is null || right.Sql.SelectQuery.From is null)
        {
            return null;
        }

        if (left.Sql.SelectQuery.From.TableReferences.Count != 1 || right.Sql.SelectQuery.From.TableReferences.Count != 1)
        {
            return null;
        }

        if (left.Sql.SelectQuery.From.Joins.Count > 0 || right.Sql.SelectQuery.From.Joins.Count > 0)
        {
            return null;
        }

        static bool NeedsDerivedTableWrapper(ExecutionPlanNode source)
            => source is SubqueryExecutionPlanNode
                || source is ProjectionExecutionPlanNode projection &&
                    (
                        projection.Top.HasValue
                        || projection.OrderBys.Count > 0
                        || projection.Offset is not null
                        || projection.Input is SubqueryExecutionPlanNode
                    )
                || source is FilterExecutionPlanNode filter && NeedsDerivedTableWrapper(filter.Input);

        static TableReference WrapAsSubquery(ExecutionPlanNode source, SelectStatement sql, string fallbackAlias)
        {
            if (source is SubqueryExecutionPlanNode sourceSubquery && sourceSubquery.IsCte && !string.IsNullOrEmpty(sourceSubquery.CteName))
            {
                var cteAlias = sourceSubquery.Alias ?? sourceSubquery.CteName;
                return new NamedTableReference(new IdentifierExpression(sourceSubquery.CteName), string.Equals(cteAlias, sourceSubquery.CteName, StringComparison.OrdinalIgnoreCase) ? null : cteAlias)
                {
                    Binding = sourceSubquery.Inner is TableScanExecutionPlanNode scan ? scan.Table : null,
                };
            }

            var derivedAlias = source switch
            {
                SubqueryExecutionPlanNode subquery => subquery.Alias ?? fallbackAlias,
                _ => fallbackAlias
            };

            return new SelectTableReference(new SelectExpression(true, sql.SelectQuery), derivedAlias);
        }

        var leftCtes = CollectCteDefinitionsFromPlan(join.Left).ToList();
        var rightCtes = CollectCteDefinitionsFromPlan(join.Right).ToList();
        var allCtes = MergeCteDefinitions(left.Sql.CommonTableExpressions, right.Sql.CommonTableExpressions, leftCtes, rightCtes).ToList();

        var leftFrom = left.Sql.SelectQuery.From ?? new FromClause([new SelectTableReference(new SelectExpression(true, left.Sql.SelectQuery), "left_subquery")], []);
        var rightFrom = right.Sql.SelectQuery.From ?? new FromClause([new SelectTableReference(new SelectExpression(true, right.Sql.SelectQuery), "right_subquery")], []);

        var leftNeedsWrapper = NeedsDerivedTableWrapper(join.Left);
        var rightNeedsWrapper = NeedsDerivedTableWrapper(join.Right);

        var leftTableReference = leftNeedsWrapper
            ? WrapAsSubquery(join.Left, left.Sql, "left_subquery")
            : leftFrom.TableReferences.SingleOrDefault() ?? new SelectTableReference(new SelectExpression(true, left.Sql.SelectQuery), "left_subquery");
        var rightTableReference = rightNeedsWrapper
            ? WrapAsSubquery(join.Right, right.Sql, "right_subquery")
            : rightFrom.TableReferences.SingleOrDefault() ?? new SelectTableReference(new SelectExpression(true, right.Sql.SelectQuery), "right_subquery");

        var trefs = leftFrom.TableReferences.ToList();
        var fromClause = join.JoinType == JoinType.Cross && join.Condition is null
            ? new FromClause(
                leftNeedsWrapper || rightNeedsWrapper ? [leftTableReference] : [.. trefs, rightTableReference],
                leftNeedsWrapper || rightNeedsWrapper
                    ? [new JoinClause(join.JoinSide, join.JoinType, rightTableReference, null)]
                    : [])
            : new FromClause(
                leftNeedsWrapper || rightNeedsWrapper ? [leftTableReference] : trefs,
                (leftNeedsWrapper || rightNeedsWrapper
                    ? new[] { new JoinClause(join.JoinSide, join.JoinType, rightTableReference, join.Condition) }
                    : leftFrom.Joins.Append(new JoinClause(join.JoinSide, join.JoinType, rightTableReference, join.Condition)).ToList()));

        var sql = new SelectExpression(
            false,
            null,
            Distinctness.Unspecified,
            left.Sql.SelectQuery.Projections.Concat(right.Sql.SelectQuery.Projections),
            fromClause,
            null,
            null,
            null,
            null,
            []);

        var select = new SelectStatement(allCtes.DistinctBy(x => x.Alias, StringComparer.OrdinalIgnoreCase), sql);

        context?.Report("SqlPushdownApplied", "Join inputs were merged into a single SQL query.", ExecutionRewriteDiagnosticLevel.Debug, nameof(JoinExecutionPlanNode), join.ToString());
        return new RemoteSqProviderSqlExecutionPlanNode(this, select);
    }

    public abstract Task<IExecutionReader> ExecuteAsync(SelectStatement sqlScript, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);

    public class TableSymbolWrapper
    {
        public TableSymbol TableSymbol { get; }
        public RemoteSqlProviderBase Provider { get; }
        public TableSymbolWrapper(TableSymbol tableSymbol, RemoteSqlProviderBase provider)
        {
            TableSymbol = tableSymbol;
            Provider = provider;
        }
    }

    public class RemoteSqProviderSqlExecutionPlanNode : ExecutionPlanNode
    {
        private readonly RemoteSqlProviderBase _provider;
        public SelectStatement Sql { get; }

        public RemoteSqProviderSqlExecutionPlanNode(RemoteSqlProviderBase provider, SelectStatement sql)
        {
            _provider = provider;
            Sql = sql;
        }

        public override IExecutionProvider? Provider => _provider;

        public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            return _provider.ExecuteAsync(Sql, parameters, cancellationToken);
        }

        public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null) => null;

        public override IEnumerable<ExecutionPlanNode> GetInputs() => [];
        public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];
    }
}