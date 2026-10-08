using System.Collections.Generic;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public void Report(string id, string message, int start, int length, Dictionary<string, object?>? state = null)
    {
        _diagnostics.Add(new Diagnostic(id, message, start, length, state));
    }

    public void Report(string id, string message, SyntaxToken token, Dictionary<string, object?>? state = null)
        => Report(id, message, new SyntaxTokenSpan(token), state);

    public void Report(string id, string message, IEnumerable<SyntaxToken> tokens, Dictionary<string, object?>? state = null)
        => Report(id, message, new SyntaxTokenSpan(tokens), state);

    public void Report(string id, string message, SyntaxTokenSpan span, Dictionary<string, object?>? state = null)
        => Report(id, message, span.Start, span.Length, state);

    public void Report(string id, string message, SyntaxNode node, Dictionary<string, object?>? state = null)
        => Report(id, message, node.Span, state);

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
}
