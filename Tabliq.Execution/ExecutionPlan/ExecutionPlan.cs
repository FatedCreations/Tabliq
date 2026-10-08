namespace Tabliq.Execution;

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