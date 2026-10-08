namespace Tabliq.Sql.Diagnostics;

public sealed record Diagnostic(string Id, string Message, int Start, int Length, IDictionary<string, object?>? State)
{
    public override string ToString() => $"{Id}: {Message} ({Start},{Length})";
}
