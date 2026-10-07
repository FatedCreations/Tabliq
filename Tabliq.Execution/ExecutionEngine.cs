using Tabliq.Execution.ExecutionReader;
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

        var statement = bound.Script.Statements.FirstOrDefault() as SelectStatement
            ?? throw new NotSupportedException("Execution engine currently supports SELECT statements only.");

        var rewriteContext = new ExecutionRewriteContext();
        var node = BuildPlan(statement.SelectQuery);
        if (statement.CommonTableExpressions.Any())
        {
            node = new CteExecutionPlanNode(node, statement.CommonTableExpressions);
        }

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
            source = new FilterExecutionPlanNode(source, select.Where.Condition);
        }

        return new ProjectionExecutionPlanNode(source, select.Projections, select);
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
            current = current is null ? next : new JoinExecutionPlanNode(current, next, JoinType.Cross, null, JoinSide.Unspecified);
        }

        foreach (var join in fromClause.Joins)
        {
            var next = BuildTableReference(join.TableReference, referencedColumns);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, join.JoinType, join.OnCondition, join.JoinSide);
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
                return new SubqueryExecutionPlanNode(BuildPlan(cte.Body), cteAlias);
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
