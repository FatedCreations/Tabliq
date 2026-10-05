using System.Reflection;
using Tabliq.Execution.ExecutionReader;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public sealed class TableScanExecutionPlanNode : ExecutionPlanNode
{
    private readonly TableSymbol _table;
    private readonly IExecutionProvider? _provider;
    private readonly string _alias;

    public TableScanExecutionPlanNode(TableSymbol table, string alias, IExecutionProvider? provider)
    {
        _table = table;
        _provider = provider;
        _alias = alias;
    }

    public string TableName => _table.TableName;

    public string? SchemaName => _table.SchemaName;

    public string Alias => _alias;

    public override IExecutionProvider? Provider => _provider;

    public override async Task<IExecutionReader> ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Must be handled/overritten by the execution provider");
    }

    public override ExecutionPlanNode? TryRewrite()
        => _provider?.TryRewrite(this);
}
