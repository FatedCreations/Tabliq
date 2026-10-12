using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Execution;

public abstract class ExecutionPlanNode
{
    private readonly string _identifier = Guid.NewGuid().ToString();

    public string Identifier => $"{this.GetType().Name}({_identifier})";

    public abstract IExecutionProvider? Provider { get; }

    public abstract IEnumerable<ExecutionPlanNode> GetInputs();

    public abstract Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);

    public abstract ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null);

    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();

    internal static ExecutionPlanNode Create(SelectExpression select)
    {
        var referencedColumns = CollectReferencedColumns(select);

        // projection!
        var projectionPlan = select.Projections
        .SelectMany(ExpandProjection)
        .ToArray();

        var whereCondition = select.Where is null ? null : ConditionExecutionPlan.Create(select.Where.Condition);

        var whereExpressions = whereCondition?.GetExpressions().SelectMany(AllExpressions) ?? [];
        var projectionExpressions = projectionPlan.SelectMany(x => AllExpressions(x.Value));
        IEnumerable<ExpressionPlanNode> allExpression = [.. projectionExpressions, .. whereExpressions];

        var current = BuildFrom(select.From, referencedColumns);

        // filtering

        // aggegate/grouping

        // filter again / having filtering

        // projection, this is where we calculate the final output values

        // sorting

        // limit / offset (top too)


        if (select.Where is not null)
        {
            current = new FilterExecutionPlanNode(current, ConditionExecutionPlan.Create(select.Where.Condition));
        }

        var aggregates = allExpression.OfType<AggregateFunctionCallExpressionExecutionPlan>().ToList();

        if (select.GroupBy is not null || aggregates.Any())
        {
            var groupBys = select.GroupBy?.Entries.Select(x => ExpressionPlanNode.Create(x)).ToArray() ?? [];

            current = new GroupByAggregateExecutionPlanNode(current, groupBys, aggregates);
        }

        if (select.OrderBy is not null)
        {
            current = new SortedExecutionPlanNode(current, select.OrderBy.Entries.Select(x =>
            new SortedPlanColumn
            {
                Expression = ExpressionPlanNode.Create(x.Expression),
                Ascending = x.Direction != OrderByDirection.Descending
            }).ToArray());
        }


        ExpressionPlanNode? fetchExpression = null;
        if (select.OrderBy?.OffsetClause?.FetchCount is not null)
        {
            fetchExpression = ExpressionPlanNode.Create(select.OrderBy.OffsetClause.FetchCount);
        }
        else if (select.Top is not null)
        {
            fetchExpression = new LiteralExpressionExecutionPlan(select.Top.Value);
        }
        ExpressionPlanNode? offsetExression = null;
        if (select.OrderBy?.OffsetClause?.OffsetCount is not null)
        {
            offsetExression = ExpressionPlanNode.Create(select.OrderBy.OffsetClause.OffsetCount);
        }

        if (offsetExression is not null || fetchExpression is not null)
        {
            // limit / offset (top too)

            current = new OffsetFetchExecutionPlanNode(current, fetchExpression, offsetExression);
        }

        current = new ProjectionExecutionPlanNode(current, projectionPlan);

        if (select.Distinctness == Distinctness.Distinct)
        {
            current = new DistinctExecutionPlanNode(current);
        }

        // unions

        foreach (var u in select.UnionStatements)
        {
            var right = Create(u.Select);
            current = new UnionAllExecutionPlanNode(current, right);
            if (!u.IsAll)
            {
                current = new DistinctExecutionPlanNode(current);
            }
        }


        //    var projectionPlan = select.Projections
        //    .SelectMany(ExpandProjection)
        //    .ToArray();

        //var projection = new ProjectionExecutionPlanNode(
        //    source,
        //    projectionPlan,
        //    select.GroupBy?.Entries.Select(x => ExpressionPlanNode.Create(x)).ToArray() ?? Array.Empty<ExpressionPlanNode>(),
        //    select.OrderBy?.Entries.Select(x => new OrderByExpressionPlan
        //    {
        //        Value = ExpressionPlanNode.Create(x.Expression),
        //        Direction = x.Direction
        //    }).ToArray() ?? Array.Empty<OrderByExpressionPlan>(),
        //    select.Top,
        //    select.OrderBy?.OffsetClause is null ? null : new OffsetExpressionPlan
        //    {
        //        OffsetCount = select.OrderBy.OffsetClause.OffsetCount is null ? null : ExpressionPlanNode.Create(select.OrderBy.OffsetClause.OffsetCount),
        //        FetchCount = select.OrderBy.OffsetClause.FetchCount is null ? null : ExpressionPlanNode.Create(select.OrderBy.OffsetClause.FetchCount)
        //    },
        //    select.Distinctness);

        return current;
        //var referencedColumns = CollectReferencedColumnsByAlias(select);
        //var source = select.From is null ? new EmptyExecutionPlanNode() : BuildFrom(select.From, referencedColumns);

        //if (select.Where is not null)
        //{
        //    source = new FilterExecutionPlanNode(source, ConditionExecutionPlan.Create(select.Where.Condition), select.Where.Condition);
        //}

        //var projectionPlan = select.Projections
        //    .SelectMany(ExpandProjection)
        //    .ToArray();

        //var projection = new ProjectionExecutionPlanNode(
        //    source,
        //    projectionPlan,
        //    select.GroupBy?.Entries.Select(x => ExpressionPlanNode.Create(x)).ToArray() ?? Array.Empty<ExpressionPlanNode>(),
        //    select.OrderBy?.Entries.Select(x => new OrderByExpressionPlan
        //    {
        //        Value = ExpressionPlanNode.Create(x.Expression),
        //        Direction = x.Direction
        //    }).ToArray() ?? Array.Empty<OrderByExpressionPlan>(),
        //    select.Top,
        //    select.OrderBy?.OffsetClause is null ? null : new OffsetExpressionPlan
        //    {
        //        OffsetCount = select.OrderBy.OffsetClause.OffsetCount is null ? null : ExpressionPlanNode.Create(select.OrderBy.OffsetClause.OffsetCount),
        //        FetchCount = select.OrderBy.OffsetClause.FetchCount is null ? null : ExpressionPlanNode.Create(select.OrderBy.OffsetClause.FetchCount)
        //    },
        //    select.Distinctness);

        //if (select.UnionStatements.Count == 0)
        //{
        //    return projection;
        //}

        //var operations = new List<(ExecutionPlanNode Input, bool IsAll)>
        //{
        //    (projection, true)
        //};

        //foreach (var union in select.UnionStatements)
        //{
        //    operations.Add((Create(union.Select), union.IsAll));
        //}

        //return new UnionExecutionPlanNode(operations);
    }

    private static IEnumerable<ExpressionPlanNode> AllExpressions(ExpressionPlanNode node)
    {
        yield return node;

        foreach (var n in node.GetExpressions())
        {
            foreach (var e in AllExpressions(n))
            {
                yield return e;
            }
        }
    }

    private static IEnumerable<ProjectionColumnPlan> ExpandProjection(SelectProjection projection)
    {
        if (projection.Expression is StarIdentifierExpression star && star.Bindings.Count > 0)
        {
            foreach (var binding in star.Bindings)
            {
                if (binding.ColumnSymbol.ExcludeFromStarExpansion)
                {
                    continue;
                }

                var identifier = new IdentifierExpression(binding.TableBinding.Alias, binding.ColumnSymbol.Name);
                identifier.WithBinding(binding);

                yield return new ProjectionColumnPlan
                {
                    Value = ExpressionPlanNode.Create(identifier),
                    Alias = binding.ColumnSymbol.Name
                };
            }

            yield break;
        }

        yield return new ProjectionColumnPlan
        {
            Value = ExpressionPlanNode.Create(projection.Expression),
            Alias = projection.Alias
        };
    }

    private static Dictionary<TableSymbol, HashSet<ColumnSymbol>> CollectReferencedColumns(SyntaxNode node)
    {
        var result = new Dictionary<TableSymbol, HashSet<ColumnSymbol>>();
        WalkForColumnReferences(node, result);
        return result;
    }

    private static void WalkForColumnReferences(SyntaxNode node, Dictionary<string, HashSet<string>> result)
    {
        if (node is IdentifierExpression identifier && identifier.Binding is { } binding)
        {
            var tableName = binding.TableBinding.TableSymbol.TableName;
            var colName = binding.ColumnSymbol.Name;
            if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(colName))
            {
                if (!result.TryGetValue(tableName, out var cols))
                    result[tableName] = cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                cols.Add(colName);
            }
        }
        else if (node is StarIdentifierExpression star)
        {
            foreach (var starBinding in star.Bindings)
            {
                var tableName = starBinding.TableBinding.TableSymbol.TableName;
                var colName = starBinding.ColumnSymbol.Name;
                if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(colName))
                {
                    if (!result.TryGetValue(tableName, out var cols))
                        result[tableName] = cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    cols.Add(colName);
                }
            }
        }

        foreach (var child in node.GetChildren())
            WalkForColumnReferences(child, result);
    }

    private static void WalkForColumnReferences(SyntaxNode node, Dictionary<TableSymbol, HashSet<ColumnSymbol>> result)
    {
        if (node is IdentifierExpression identifier && identifier.Binding is { } binding)
        {
            var table = binding.TableBinding.TableSymbol;
            var colName = binding.ColumnSymbol;
            if (table is not null && colName is not null)
            {
                if (!result.TryGetValue(table, out var cols))
                    result[table] = cols = new HashSet<ColumnSymbol>();
                cols.Add(colName);
            }
        }
        else if (node is StarIdentifierExpression star)
        {
            foreach (var starBinding in star.Bindings)
            {
                var table = starBinding.TableBinding.TableSymbol;
                var colName = starBinding.ColumnSymbol;
                if (table is not null && colName is not null)
                {
                    if (!result.TryGetValue(table, out var cols))
                        result[table] = cols = new HashSet<ColumnSymbol>();
                    cols.Add(colName);
                }
            }
        }

        foreach (var child in node.GetChildren())
            WalkForColumnReferences(child, result);
    }

    private static ExecutionPlanNode BuildFrom(FromClause? fromClause, Dictionary<TableSymbol, HashSet<ColumnSymbol>> referencedColumns)
    {
        if (fromClause is null)
        {
            return new EmptyExecutionPlanNode();
        }

        ExecutionPlanNode? current = null;

        // CollectReferencedColumnsByAlias()
        foreach (var tableReference in fromClause.TableReferences)
        {
            var next = BuildTableReference(tableReference, referencedColumns);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, JoinType.Cross, null, JoinSide.Unspecified);
        }

        foreach (var join in fromClause.Joins)
        {
            var next = BuildTableReference(join.TableReference, referencedColumns);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, join.JoinType,
                join.OnCondition is null ? null : ConditionExecutionPlan.Create(join.OnCondition),
                join.JoinSide);
        }

        return current ?? new EmptyExecutionPlanNode();
    }

    private static ExecutionPlanNode BuildTableReference(TableReference tableReference, Dictionary<TableSymbol, HashSet<ColumnSymbol>> referencedColumns)
    {
        if (tableReference is NamedTableReference namedTableReference)
        {
            var table = namedTableReference.Binding ?? throw new InvalidOperationException($"Table '{namedTableReference.Identifer}' was not bound.");
            if (table.GetState<CteTableMetadata>() is CteTableMetadata cte)
            {
                var cteAlias = namedTableReference.Alias ?? cte.Alias;
                return new SubqueryExecutionPlanNode(Create(cte.Body), cteAlias, cte.Alias);
            }

            var tableAlias = namedTableReference.Alias ?? table.TableName;

            return CreateTableScan(table, tableAlias, referencedColumns);
        }

        if (tableReference is SelectTableReference subSelect)
        {
            return new SubqueryExecutionPlanNode(Create(subSelect.Select), subSelect.Alias, null);
        }

        throw new NotSupportedException($"Unsupported table reference type: {tableReference.GetType().Name}");
    }

    private static ExecutionPlanNode CreateTableScan(TableSymbol table, string alias, Dictionary<TableSymbol, HashSet<ColumnSymbol>> referencedColumns)
    {
        var provider = ResolveProvider(table);

        if (referencedColumns.TryGetValue(table, out var colsSet))
        {
            return new TableScanExecutionPlanNode(table, alias, provider, colsSet.ToList());
        }
        return new TableScanExecutionPlanNode(table, alias, provider, table.Columns);
    }

    private static IExecutionProvider? ResolveProvider(TableSymbol table)
    {
        if (table.GetState<ExecutionProviderTableMetadata>() is ExecutionProviderTableMetadata metadata)
        {
            return metadata.ExecutionProvider;
        }

        return null;
    }
}
