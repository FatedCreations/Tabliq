using Tabliq.Sql.Binding;

namespace Tabliq.Execution.SqlServer;

public class VirtualSchema
{
    public IReadOnlyList<VirtualTable> Tables { get; set; } = [];

    public IReadOnlyList<FunctionSymbol> Functions { get; set; } = [];
}
