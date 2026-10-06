using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

// this is logically a wrapper around another ExecutionPlanNode, all its responsible for is aliasing the columns on the way thru
public sealed class SubqueryExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _inner;
    private readonly string? _alias;

    public SubqueryExecutionPlanNode(ExecutionPlanNode inner, string? alias)
    {
        _inner = inner;
        _alias = alias;
    }

    public ExecutionPlanNode Inner => _inner;

    public override IExecutionProvider? Provider => _inner.Provider;

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInner = _inner.TryRewrite(context) ?? _inner;
        if (newInner != _inner)
        {
            currentNode = new SubqueryExecutionPlanNode(newInner, _alias);
        }

        currentNode = newInner?.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => new AliasedExecutionReader(_alias, await _inner.ExecuteAsync(parameters, cancellationToken));
}
