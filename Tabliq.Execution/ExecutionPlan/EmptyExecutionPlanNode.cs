using Tabliq.Execution.ExecutionReader;

namespace Tabliq.Execution;

public sealed class EmptyExecutionPlanNode : ExecutionPlanNode
{
    public override Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
        => Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(Array.Empty<string>(), new List<object?[]?>().GetEnumerator(), Array.Empty<IAsyncDisposable>()));

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite()
        => null;
}
