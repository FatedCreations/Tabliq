using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;

namespace Tabliq.Execution;

public sealed class EmptyExecutionPlanNode : ExecutionPlanNode
{
    public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IExecutionReader>(new EnumeratorExecutionReader(
            Array.Empty<string>(),
            Enumerable.Repeat(Array.Empty<object?>(), 1).GetEnumerator(),
            Array.Empty<IAsyncDisposable>()));

    public override IExecutionProvider? Provider => null;
    public override IEnumerable<ExecutionPlanNode> GetInputs() => [];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
        => null;
}
