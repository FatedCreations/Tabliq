using Tabliq.Sql.Ast;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Printer;
using static System.Net.Mime.MediaTypeNames;

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
            throw new CompilationDiagnosticsException(Text, Diagnostics);
        }
    }
}


public class CompilationDiagnosticsException : Exception
{
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public CompilationDiagnosticsException(string text, IReadOnlyList<Diagnostic> diagnostics)
        : base($"Invalid SQL: {text}. Diagnostics: {string.Join(", ", diagnostics.Select(d => d.Message))}")
    {
        Diagnostics = diagnostics;
    }
}