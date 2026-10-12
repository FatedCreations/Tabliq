namespace Tabliq.Execution;

public enum ExecutionRewriteDiagnosticLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

public sealed record ExecutionRewriteDiagnostic(
    string Id,
    string Message,
    ExecutionRewriteDiagnosticLevel Level = ExecutionRewriteDiagnosticLevel.Info,
    string? Source = null,
    string? Details = null)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Source)
        ? $"[{Level}] {Id}: {Message}"
        : $"[{Level}] {Id}: {Message} ({Source})";
}

public sealed class ExecutionRewriteContext
{
    private readonly List<ExecutionRewriteDiagnostic> _diagnostics = new();

    public IReadOnlyList<ExecutionRewriteDiagnostic> Diagnostics => _diagnostics;

    public void Report(string id, string message, string? source = null, string? details = null)
        => Report(id, message, ExecutionRewriteDiagnosticLevel.Info, source, details);

    public void Report(string id, string message, ExecutionRewriteDiagnosticLevel level, string? source = null, string? details = null)
        => _diagnostics.Add(new ExecutionRewriteDiagnostic(id, message, level, source, details));
}
