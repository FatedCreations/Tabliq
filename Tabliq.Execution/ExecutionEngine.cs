using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Execution.Functions;
using Tabliq.Execution.Policies;
using Tabliq.Execution.Providers;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Tabliq.Sql.Parsing;
using Binder = Tabliq.Sql.Binding.Binder;

namespace Tabliq.Execution;

public class ExecutionEngine
{
    public List<IExecutionPolicy> Policies { get; set; } = [];

    private readonly IEnumerable<IExecutionProvider> _providers;
    private readonly IEnumerable<SqlFunction> _functions;

    public ExecutionEngine(IEnumerable<IExecutionProvider> providers, IEnumerable<SqlFunction>? functions = null)
    {
        _providers = providers;
        _functions = BuiltinFunctions.BuiltingFunctions;
        if (functions is not null)
        {
            _functions = [.. functions, .. BuiltinFunctions.BuiltingFunctions];
        }
    }

    public async Task<IExecutionReader> ExecuteAsync(string sql, IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        var plan = BuildPlan(sql, parameters);

        return await plan.ExecuteAsync(parameters, cancellationToken);
    }


    public ExecutionPlan BuildPlan(string sql, IEnumerable<ExecuterParameter>? parameters = null)
    {
        parameters ??= Enumerable.Empty<ExecuterParameter>();

        var schema = new ExecutionSchemaProvider(_providers, parameters, _functions);
        var compilation = Parser.Parse(sql);
        compilation.ThrowIfInvalid();

        var bound = Binder.Bind(compilation, schema);
        bound.ThrowIfInvalid();

        var statements = bound.Script.Statements.ToList();
        if (statements.Count != 1 || statements[0] is not SelectStatement statement)
        {
            throw new NotSupportedException($"Execution engine currently supports exactly one SELECT statement at a time; found {statements.Count} statement(s).");
        }

        var rewriteContext = new ExecutionRewriteContext();
        var node = BuildPlan(statement.SelectQuery);

        node = node.TryRewrite(rewriteContext) ?? node;

        var plan = new ExecutionPlan(node, rewriteContext.Diagnostics);

        IEnumerable<PolicyValidationError> accumulatedErrors = Enumerable.Empty<PolicyValidationError>();
        foreach(var p in Policies)
        {
            if(!p.Validate(plan, out var errors))
            {
                accumulatedErrors = accumulatedErrors.Concat(errors);
                // Handle validation errors (e.g., throw an exception, log, etc.)
            }
        }

        if (accumulatedErrors.Any())
        {
            throw new PolicyValidationException(accumulatedErrors);
        }

        return plan;
    }


    private static ExecutionPlanNode BuildPlan(SelectExpression select)
    {
        var referencedColumns = CollectReferencedColumnsByAlias(select);
        var source = select.From is null ? new EmptyExecutionPlanNode() : BuildFrom(select.From, referencedColumns);

        if (select.Where is not null)
        {
            source = new FilterExecutionPlanNode(source, BuildConditionPlan(select.Where.Condition), select.Where.Condition);
        }

        var projection = new ProjectionExecutionPlanNode(
            source,
            select.Projections.Select(x => BuildExpressionPlan(x.Expression)).ToArray(),
            select.Projections,
            select);
        if (select.UnionStatements.Count == 0)
        {
            return projection;
        }

        var operations = new List<(ExecutionPlanNode Input, bool IsAll)>
        {
            (projection, true)
        };

        foreach (var union in select.UnionStatements)
        {
            operations.Add((BuildPlan(union.Select), union.IsAll));
        }

        return new UnionExecutionPlanNode(operations);
    }

    private static ExpressionPlanNode BuildExpressionPlan(Expression expression)
    {
        if (expression is SelectExpression s)
        {
            throw new Exception("Unexpected SelectExpression");
        }

        return expression switch
        {
            LiteralExpression literal => new LiteralExpressionExecutionPlan(literal.Value),
            IdentifierExpression identifier => new IdentifierExpressionExecutionPlan(identifier),
            BracketedExpression bracketed => BuildExpressionPlan(bracketed.Expression),
            InExpression inExpression => new SubValueInExpressionExecutionPlan(BuildExpressionPlan(inExpression.Expression), BuildExpressionPlan(inExpression.SubValue)),
            AsExpression asExpression => new ConvertExpressionExecutionPlan(BuildExpressionPlan(asExpression.Expression), asExpression.DataType),
            ValueFromExpression valueFromExpression => new ValuePartExpressionPlanNode(BuildExpressionPlan(valueFromExpression.Expression), valueFromExpression.Part),
            BinaryOperatorExpression binary => new BinaryOperatorExpressionExecutionPlan(BuildExpressionPlan(binary.Left), binary.Operator, BuildExpressionPlan(binary.Right)),
            UnaryOperatorExpression unary => new UnaryOperatorExpressionExecutionPlan(BuildExpressionPlan(unary.Expression), unary.Operator),
            // CaseExpression caseExpression => new CaseExpressionExecutionPlan(caseExpression),
            FunctionCallExpression functionCall => functionCall.Binding?.GetState<SqlFunction>() switch
            {
                AggregateFunction agg => new AggregateFunctionCallExpressionExecutionPlan(functionCall, agg, functionCall.Arguments.Select(BuildExpressionPlan)),
                ValueFunction valueFunc => new ValueFunctionCallExpressionExecutionPlan(functionCall, valueFunc, functionCall.Arguments.Select(BuildExpressionPlan)),
                _ => throw new NotSupportedException($"Unsupported function type for function '{functionCall.FunctionName}': {functionCall.Binding?.GetType().Name ?? "null"}")
            },
            _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    private static ConditionExecutionPlan BuildConditionPlan(Condition expression)
    {
        return expression switch
        {
            BinaryComparisonCondition comparison => new BinaryComparisonConditionExecutionPlan(BuildExpressionPlan(comparison.Left), comparison.Operator, BuildExpressionPlan(comparison.Right)),
            LogicalCondition logical => new LogicalConditionExecutionPlan(BuildConditionPlan(logical.Left), logical.Operator, BuildConditionPlan(logical.Right)),
            BracketedCondition bracketed => new BracketedConditionExecutionPlan(BuildConditionPlan(bracketed.Expression)),
            UnaryCondition unary => new UnaryConditionExecutionPlan(BuildConditionPlan(unary.Right), unary.Operator),
            IsNullCondition isNull => new IsNullConditionExecutionPlan(BuildExpressionPlan(isNull.Expression), isNull.IsNot),
            LikeCondition like => new LikeConditionExecutionPlan(BuildExpressionPlan(like.Left), BuildExpressionPlan(like.Right), like.IsNot),
            BetweenCondition between => new BetweenConditionExecutionPlan(BuildExpressionPlan(between.Left), BuildExpressionPlan(between.From), BuildExpressionPlan(between.To), between.IsNot),
            InListCondition inList => new InListConditionExecutionPlan(BuildExpressionPlan(inList.Left), inList.Items.Select(BuildExpressionPlan), inList.IsNot),
            InSelectCondition inSelect => new InSelectConditionExecutionPlan(BuildPlan(inSelect.Expression)),
            ExistsCondition exists => new ExistsConditionExecutionPlan(BuildPlan(exists.SelectExpression)),
            _ => throw new NotSupportedException($"Unsupported condition type: {expression.GetType().Name}")
        };
    }

    private static Dictionary<string, HashSet<string>> CollectReferencedColumnsByAlias(SyntaxNode node)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        WalkForColumnReferences(node, result);
        return result;
    }

    private static void WalkForColumnReferences(SyntaxNode node, Dictionary<string, HashSet<string>> result)
    {
        if (node is IdentifierExpression identifier && identifier.Binding is { } binding)
        {
            var tableName = binding.TableSymbol.TableName;
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
                var tableName = starBinding.TableSymbol.TableName;
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

    private static ExecutionPlanNode BuildFrom(FromClause fromClause, Dictionary<string, HashSet<string>> referencedColumns)
    {
        ExecutionPlanNode? current = null;

        foreach (var tableReference in fromClause.TableReferences)
        {
            var next = BuildTableReference(tableReference, referencedColumns);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, JoinType.Cross, null, null, JoinSide.Unspecified);
        }

        foreach (var join in fromClause.Joins)
        {
            var next = BuildTableReference(join.TableReference, referencedColumns);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, join.JoinType,
                join.OnCondition is null ? null : BuildConditionPlan(join.OnCondition),
                join.OnCondition,
                join.JoinSide);
        }

        return current ?? new EmptyExecutionPlanNode();
    }

    private static ExecutionPlanNode BuildTableReference(TableReference tableReference, Dictionary<string, HashSet<string>> referencedColumns)
    {
        if (tableReference is NamedTableReference namedTableReference)
        {
            var table = namedTableReference.Binding ?? throw new InvalidOperationException($"Table '{namedTableReference.Identifer}' was not bound.");
            if (table.GetState<CteTableMetadata>() is CteTableMetadata cte)
            {
                var cteAlias = namedTableReference.Alias ?? cte.Alias;
                return new SubqueryExecutionPlanNode(BuildPlan(cte.Body), cteAlias, cte.Alias, cte.Body);
            }

            var tableAlias = namedTableReference.Alias ?? table.TableName;
            referencedColumns.TryGetValue(tableAlias, out var cols);
            return CreateTableScan(table, tableAlias, cols);
        }

        if (tableReference is SelectTableReference subSelect)
        {
            return new SubqueryExecutionPlanNode(BuildPlan(subSelect.Select), subSelect.Alias);
        }

        throw new NotSupportedException($"Unsupported table reference type: {tableReference.GetType().Name}");
    }

    private static ExecutionPlanNode CreateTableScan(TableSymbol table, string alias, IReadOnlySet<string>? referencedColumns = null)
    {
        var provider = ResolveProvider(table);

        var cols = table.Columns.Where(x => referencedColumns?.Contains(x.Name, StringComparer.OrdinalIgnoreCase) ?? false).ToList();
        return new TableScanExecutionPlanNode(table, alias, provider, cols);
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
