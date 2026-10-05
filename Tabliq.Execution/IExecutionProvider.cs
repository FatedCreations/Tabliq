using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;

namespace Tabliq.Execution;

public interface IExecutionProvider
{
    IEnumerable<TableSymbol> GetTables();

    TableSymbol? GetTable(string tableName, string? schemaName = null);
    ExecutionPlanNode TryRewrite(ExecutionPlanNode node);
}
