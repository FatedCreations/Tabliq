namespace Tabliq.Execution;

public sealed record ExecutionRewriteDiagnostic(string Id, string Message, string? Source = null, string? Details = null)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Source)
        ? $"{Id}: {Message}"
        : $"{Id}: {Message} ({Source})";
}

public sealed class ExecutionRewriteContext
{
    private readonly List<ExecutionRewriteDiagnostic> _diagnostics = new();

    public IReadOnlyList<ExecutionRewriteDiagnostic> Diagnostics => _diagnostics;

    public void Report(string id, string message, string? source = null, string? details = null)
        => _diagnostics.Add(new ExecutionRewriteDiagnostic(id, message, source, details));
}
