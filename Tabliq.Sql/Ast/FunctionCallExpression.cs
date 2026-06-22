using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class FunctionCallExpression : Expression
{
    public FunctionCallExpression(string functionName, IEnumerable<Expression> arguments, WindowSpecification? window)
    {
        FunctionName = functionName;
        Arguments = new List<Expression>(arguments);
        Window = window;
    }

    public string FunctionName { get; }
    public IReadOnlyList<Expression> Arguments { get; }
    public WindowSpecification? Window { get; }
    public FunctionSymbol? Binding { get; set; }

    public override bool Equals(SyntaxNode? other)
    {
        return other is FunctionCallExpression fc &&
            ((fc.Window is null && Window is null) || Window?.Equals(fc.Window) == true) &&
            Arguments.SyntaxSequenceEqual(fc.Arguments);
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Arguments)
        {
            yield return a;
        }

        if (Window is not null)
        {
            yield return Window;
        }
    }
}