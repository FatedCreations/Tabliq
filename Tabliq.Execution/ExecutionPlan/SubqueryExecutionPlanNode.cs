using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class SubqueryExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _inner;
    private readonly string? _alias;

    public SubqueryExecutionPlanNode(ExecutionPlanNode inner, string? alias)
    {
        _inner = inner;
        _alias = alias;
    }

    public override IExecutionProvider? Provider => null;

    public override ExecutionPlanNode? TryRewrite()
    {
        ExecutionPlanNode currentNode = this;
        var newInner = _inner.TryRewrite() ?? _inner;
        if (newInner != _inner)
        {
            currentNode = new SubqueryExecutionPlanNode(newInner, _alias);
        }

        currentNode = newInner?.Provider?.TryRewrite(currentNode) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
        => new AliasedExecutionReader(_alias, await _inner.ExecuteAsync(cancellationToken));
}
