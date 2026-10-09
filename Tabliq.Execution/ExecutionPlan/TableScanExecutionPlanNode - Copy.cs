//using System.Reflection;
//using Tabliq.Execution.ExecutionReader;
//using Tabliq.Execution.ExpressionPlan;
//using Tabliq.Sql.Binding;

//namespace Tabliq.Execution;

//public sealed class TableScanExecutionPlanNode : ExecutionPlanNode
//{
//    private readonly TableSymbol _table;
//    private readonly IExecutionProvider? _provider;
//    private readonly string _alias;

//    public TableScanExecutionPlanNode(TableSymbol table, string alias, IExecutionProvider? provider, IReadOnlyList<ColumnSymbol>? referencedColumns = null)
//    {
//        _table = table;
//        _provider = provider;
//        _alias = alias;
//        ReferencedColumns = referencedColumns;
//    }

//    public override IEnumerable<ExecutionPlanNode> GetInputs() => Enumerable.Empty<ExecutionPlanNode>();
//    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [];

//    public string TableName => _table.TableName;

//    public string? SchemaName => _table.SchemaName;

//    public TableSymbol Table => _table;

//    public IReadOnlyList<ColumnSymbol> Columns => _table.Columns;

//    public string Alias => _alias;

//    /// <summary>The set of columns referenced from this table in the query. Null means all columns are needed.</summary>
//    public IReadOnlyList<ColumnSymbol>? ReferencedColumns { get; }

//    public override IExecutionProvider? Provider => _provider;

//    public override async Task<IExecutionReader> ExecuteAsync(IEnumerable<ExecuterParameter>? parameters = null, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException("Must be handled/overritten by the execution provider");
//    }

//    public override ExecutionPlanNode? TryRewrite(ExecutionRewriteContext? context = null)
//        => _provider?.TryRewrite(this, context);
//}
