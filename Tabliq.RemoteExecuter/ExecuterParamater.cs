using System.Diagnostics.CodeAnalysis;
using Tabliq.Sql.Binding;

namespace Tabliq.RemoteExecuter;

public class ExecuterParameter
{
    public ExecuterParameter()
    {
    }

    [SetsRequiredMembers]
    public ExecuterParameter(string name, object? value)
    {
        Name = name;
        Value = value;
    }

    public required string Name { get; set; }

    public required object? Value { get; set; }

    internal ParameterSymbol AsSymbol()
        => new ParameterSymbol(Name, "")
        {
            State = this
        };
}
