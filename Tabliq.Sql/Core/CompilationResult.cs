using Tabliq.Sql.Ast;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Printer;

namespace Tabliq.Sql.Core;

public sealed class CompilationResult
{
    private string? _text;
    public string Text => _text ??= SqlWriter.ToSql(Script);
    public SqlScript Script { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }

    internal CompilationResult(string text, SqlScript root, IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<Diagnostic> diagnostics)
    {
        _text = text;
        Script = root;
        Diagnostics = diagnostics;
        Tokens = tokens;
    }

    internal CompilationResult(SqlScript root, IReadOnlyList<Diagnostic> diagnostics)
    {
        _text = null;
        Script = root;
        Diagnostics = diagnostics;
        Tokens = [];
    }

    public void ThrowIfInvalid()
    {
        if (Diagnostics.Any())
        {
            throw new Exception($"Invalid SQL: {Text}. Diagnostics: {string.Join(", ", Diagnostics.Select(d => d.Message))}");
        }
    }
}
