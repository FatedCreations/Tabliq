using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class ParameterIdentifier : Expression
{
    public ParameterIdentifier(string paramterName)
    {
        ParamterName = paramterName;
    }

    public string ParamterName { get; }

    public ParameterBinding? Binding { get; set; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
    public override bool Equals(SyntaxNode? other)
    {
        return other is ParameterIdentifier otherParameter && Equals(ParamterName, otherParameter.ParamterName);
    }
}
