using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tabliq.Execution;

public interface IExecutionProvider
{
    IEnumerable<TableSymbol> GetTables();

    TableSymbol? GetTable(string tableName, string? schemaName = null);

    FunctionSymbol? GetFunction(string functionName);

    Task<IExecutionReader> ReadTableAsync(string tableName, string? schemaName = null, CancellationToken cancellationToken = default);
}
