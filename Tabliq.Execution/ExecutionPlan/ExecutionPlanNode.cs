namespace Tabliq.Execution;

public abstract class ExecutionPlanNode
{
    public abstract IExecutionProvider? Provider { get; }

    public abstract Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default);

    public abstract ExecutionPlanNode? TryRewrite();
}
