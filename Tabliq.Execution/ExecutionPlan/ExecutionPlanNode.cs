using Tabliq.Execution.ExpressionPlan;

namespace Tabliq.Execution;

public abstract class ExecutionPlanNode
{
    public abstract IExecutionProvider? Provider { get; }

    public abstract IEnumerable<ExecutionPlanNode> GetInputs();

    public abstract Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);

    public abstract ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null);

    public abstract IEnumerable<ExpressionPlanNode> GetExpressions();
}

public class ExecutionPlan
{
    public ExecutionPlan(ExecutionPlanNode rootNode, IReadOnlyList<ExecutionRewriteDiagnostic> diagnostic)
    {
        Diagnostics = diagnostic;
        RootNode = rootNode;
    }

    public IReadOnlyList<ExecutionRewriteDiagnostic> Diagnostics { get; }

    public ExecutionPlanNode RootNode { get; }

    public Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => RootNode.ExecuteAsync(parameters, cancellationToken);
}