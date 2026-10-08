using Tabliq.Execution.Functions;
using Tabliq.Execution.Policies;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Parsing;

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
        var node = ExecutionPlanNode.Create(statement.SelectQuery);

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
}
