using System.Net.NetworkInformation;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Printer;

namespace Tabliq.Sql.Core;

public sealed class CompilationResult : CompilationResult<SqlScript>
{
    internal CompilationResult(string text, SqlScript root, IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<Diagnostic> diagnostics)
        :base(text, root, tokens, diagnostics)
    {
    }

    internal CompilationResult(SqlScript root, IReadOnlyList<Diagnostic> diagnostics)
        : base(root,diagnostics)
    {
    }
}

public  class CompilationResult<T>
    where T : SyntaxNode
{
    private string? _text;
    public string Text => _text ??= new SqlWriter().ToSql(Script);
    public T Script { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }

    internal CompilationResult(string text, T root, IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<Diagnostic> diagnostics)
    {
        _text = text;
        Script = root;
        Diagnostics = diagnostics;
        Tokens = tokens;
    }
    internal CompilationResult(T root, IReadOnlyList<Diagnostic> diagnostics)
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
        : base($"Invalid SQL: {text}. Diagnostics: {string.Join(", ", diagnostics.Select(d => GetDiagnosticMessage(text, d)))}")
    {
        Diagnostics = diagnostics;
    }
    private static string GetDiagnosticMessage(string text, Diagnostic d)
    {
        var prefix = text[Math.Max(0, d.Start - 10)..d.Start];
        var postfix = text[(d.Start + d.Length)..Math.Min(d.Start + d.Length + 10, text.Length)];
        var slice = text[d.Start..(d.Start + d.Length)];

        return $"{d.Id} : [{d.Start}:{d.Length}] : {d.Message} `{prefix}|>{slice}<|{postfix}`";
    }
}