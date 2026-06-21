using System.Collections.Generic;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public void Report(string id, string message, int start, int length)
    {
        _diagnostics.Add(new Diagnostic(id, message, start, length));
    }

    public void Report(string id, string message, SyntaxToken token)
        => Report(id, message, new SyntaxTokenSpan(token));

    public void Report(string id, string message, IEnumerable<SyntaxToken> tokens)
        => Report(id, message, new SyntaxTokenSpan(tokens));

    public void Report(string id, string message, SyntaxTokenSpan span)
        => Report(id, message, span.Start, span.Length);

    public void Report(string id, string message, SyntaxNode node)
        => Report(id, message, node.Span);

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
}
