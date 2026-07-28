using Tabliq.Sql.Binding;

namespace Tabliq.RemoteExecuter;

public class VirtualSchema
{
    public IReadOnlyList<VirtualTable> Tables { get; set; } = [];

    public IReadOnlyList<FunctionSymbol> Functions { get; set; } = [];
}
