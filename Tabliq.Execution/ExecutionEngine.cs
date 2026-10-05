using System.Reflection;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Parsing;
using Binder = Tabliq.Sql.Binding.Binder;

namespace Tabliq.Execution;

public class ExecutionEngine
{
    private readonly IEnumerable<IExecutionProvider> _providers;

    public ExecutionEngine(IEnumerable<IExecutionProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IExecutionReader> ExecuteAsync(string sql, IEnumerable<ExecuterParameter> parameters, CancellationToken cancellationToken)
    {
        parameters ??= Enumerable.Empty<ExecuterParameter>();

        var schema = new ExecutionSchemaProvider(_providers, parameters);
        var compilation = Parser.Parse(sql);
        compilation.ThrowIfInvalid();

        var bound = Binder.Bind(compilation, schema);
        bound.ThrowIfInvalid();

        var statement = bound.Script.Statements.FirstOrDefault() as SelectStatement
            ?? throw new NotSupportedException("Execution engine currently supports SELECT statements only.");

        var plan = BuildPlan(statement.SelectQuery);
        plan = plan.TryRewrite() ?? plan;

        return await plan.ExecuteAsync(cancellationToken);
    }


    private static ExecutionPlanNode BuildPlan(SelectExpression select)
    {
        var source = select.From is null ? new EmptyExecutionPlanNode() : BuildFrom(select.From);

        if (select.Where is not null)
        {
            source = new FilterExecutionPlanNode(source, select.Where.Condition);
        }

        return new ProjectionExecutionPlanNode(source, select.Projections, select);
    }

    private static ExecutionPlanNode BuildFrom(FromClause fromClause)
    {
        ExecutionPlanNode? current = null;

        foreach (var tableReference in fromClause.TableReferences)
        {
            var next = BuildTableReference(tableReference);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, JoinType.Cross, null, JoinSide.Unspecified);
        }

        foreach (var join in fromClause.Joins)
        {
            var next = BuildTableReference(join.TableReference);
            current = current is null ? next : new JoinExecutionPlanNode(current, next, join.JoinType, join.OnCondition, join.JoinSide);
        }

        return current ?? new EmptyExecutionPlanNode();
    }

    private static ExecutionPlanNode BuildTableReference(TableReference tableReference)
    {
        if (tableReference is NamedTableReference namedTableReference)
        {
            var table = namedTableReference.Binding ?? throw new InvalidOperationException($"Table '{namedTableReference.Identifer}' was not bound.");
            return CreateTableScan(table, namedTableReference.Alias ?? table.TableName);
        }

        if (tableReference is SelectTableReference subSelect)
        {
            return new SubqueryExecutionPlanNode(BuildPlan(subSelect.Select), subSelect.Alias);
        }

        throw new NotSupportedException($"Unsupported table reference type: {tableReference.GetType().Name}");
    }

    private static ExecutionPlanNode CreateTableScan(TableSymbol table, string alias)
    {
        var provider = ResolveProvider(table);
        return new TableScanExecutionPlanNode(table, alias, provider);
    }

    private static IExecutionProvider? ResolveProvider(TableSymbol table)
    {
        if (table.State is ExecutionProviderTableMetadata metadata)
        {
            return metadata.ExecutionProvider;
        }

        return null;
    }
}
