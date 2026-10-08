using Tabliq.Execution.ExecutionReader;
using Tabliq.Execution.ExpressionPlan;
using Tabliq.Sql.Ast;

namespace Tabliq.Execution;

// this is logically a wrapper around another ExecutionPlanNode, all its responsible for is aliasing the columns on the way thru
public sealed class SubqueryExecutionPlanNode : ExecutionPlanNode
{
    private readonly ExecutionPlanNode _inner;
    private readonly string? _alias;
    private readonly string? _cteName;
    private readonly SelectExpression? _cteBody;
    private readonly int? _declarationOrder;

    public SubqueryExecutionPlanNode(ExecutionPlanNode inner, string? alias, SelectExpression? cteBody = null)
        : this(inner, alias, cteBody is not null ? alias : null, cteBody, null)
    {
    }

    public SubqueryExecutionPlanNode(ExecutionPlanNode inner, string? alias, string? cteName, SelectExpression? cteBody, int? declarationOrder = null)
    {
        _inner = inner;
        _alias = alias;
        _cteName = cteName;
        _cteBody = cteBody;
        _declarationOrder = declarationOrder;
    }

    public ExecutionPlanNode Inner => _inner;
    public string? Alias => _alias;
    public string? CteName => _cteName;
    public SelectExpression? CteBody => _cteBody;
    public int? DeclarationOrder => _declarationOrder;
    public bool IsCte => _cteBody is not null && !string.IsNullOrEmpty(_cteName);

    public override IExecutionProvider? Provider => _inner.Provider;
    public override IEnumerable<ExecutionPlanNode> GetInputs() => [_inner];
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [.. _inner.GetExpressions()];

    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
    {
        ExecutionPlanNode currentNode = this;
        var newInner = _inner.TryRewrite(context) ?? _inner;
        if (newInner != _inner)
        {
            currentNode = new SubqueryExecutionPlanNode(newInner, _alias, _cteName, _cteBody, _declarationOrder);
        }

        currentNode = newInner.Provider?.TryRewrite(currentNode, context) ?? currentNode;

        return currentNode;
    }

    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
        => new AliasedExecutionReader(_alias, await _inner.ExecuteAsync(parameters, cancellationToken));
}
