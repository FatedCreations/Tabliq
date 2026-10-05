namespace Tabliq.Execution;

public abstract class ExecutionPlanNode
{
    public abstract IExecutionProvider? Provider { get; }

    public abstract Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken);

    public abstract ExecutionPlanNode? TryRewrite();
}
