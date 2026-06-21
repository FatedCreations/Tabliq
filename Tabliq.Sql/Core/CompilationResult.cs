using Tabliq.Sql.Ast;
using Tabliq.Sql.Diagnostics;

namespace Tabliq.Sql.Core;

public sealed class CompilationResult
{
    public string Text { get; }
    public SqlScript Script { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }

    internal CompilationResult(string text, SqlScript root, IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<Diagnostic> diagnostics)
    {
        Text = text;
        Script = root;
        Diagnostics = diagnostics;
        Tokens = tokens;
    }
}
