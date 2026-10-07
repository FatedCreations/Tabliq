using Tabliq.Execution.ExecutionReader;

namespace Tabliq.Execution;

public sealed class EmptyExecutionPlanNode : ExecutionPlanNode
{
    public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(Array.Empty<string>(), new List<object?[]?>().GetEnumerator(), Array.Empty<IAsyncDisposable>()));

    public override IExecutionProvider? Provider => null;
    public override IEnumerable<ExecutionPlanNode> GetInputs() => [];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
        => null;
}
