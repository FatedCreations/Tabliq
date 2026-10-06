using Tabliq.Sql.Binding;

namespace Tabliq.Execution.Functions;

public abstract class SqlFunction
{
    internal SqlFunction(
        string name,
        bool isAggregate,
        IEnumerable<FunctionArgument> arguments,
        FunctionArgument? paramsArgument = null)
    {
        Name = name;
        IsAggregate = isAggregate;
        Arguments = arguments.ToList();
        ParamsArgument = paramsArgument;

        FunctionSymbol = new FunctionSymbol(
            name,
            isAggregate,
            arguments.Select(a => new FunctionArgumentSymbol(
                a.Name,
                a.RequiredType,
                a.BinderHandling,
                a.Optional)).ToList(),
            paramsArgument != null ? new FunctionArgumentSymbol(
                paramsArgument.Name,
                paramsArgument.RequiredType,
                paramsArgument.BinderHandling,
                paramsArgument.Optional) : null)
        .WithState(this);
    }

    public string Name { get; }
    public bool IsAggregate { get; }
    public IReadOnlyList<FunctionArgument> Arguments { get; }
    public FunctionArgument? ParamsArgument { get; }

    public FunctionSymbol FunctionSymbol { get; }

    public sealed record FunctionArgument(string Name, Type? RequiredType = null, BinderHandling BinderHandling = BinderHandling.Bind, bool Optional = false);
}
