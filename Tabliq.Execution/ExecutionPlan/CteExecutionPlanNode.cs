using Tabliq.Execution.Providers;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

public sealed class CteExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _inner;
    private readonly IReadOnlyList<CommonTableExpression> _commonTableExpressions;

    public CteExecutionPlanNode(ExecutionPlanNode inner, IEnumerable<CommonTableExpression> commonTableExpressions)
    {
        _inner = inner;
        _commonTableExpressions = commonTableExpressions.ToList();
    }

    public override IExecutionProvider? Provider => _inner.Provider;

    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_inner];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        var rewritten = _inner.TryRewrite(context) ?? _inner;
        if (rewritten is RemoteSqlProviderBase.RemoteSqProviderSqlExecutionPlanNode remote && remote.Provider is RemoteSqlProviderBase provider)
        {
            return new RemoteSqlProviderBase.RemoteSqProviderSqlExecutionPlanNode(provider, new SelectStatement(_commonTableExpressions, remote.Sql.SelectQuery));
        }

        return rewritten;
    }

    public override Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => _inner.ExecuteAsync(parameters, cancellationToken);
}