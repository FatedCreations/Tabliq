using System.Diagnostics;

namespace Tabliq.Sql.Core;

[DebuggerDisplay("{Text} ({Kind})")]
public sealed class SyntaxToken
{
    public SyntaxKind Kind { get; }
    public string Text { get; }
    public object? Value { get; }
    public int Start { get; }
    public int End => Start + Text.Length;

    public SyntaxToken(SyntaxKind kind, string text, object? value, int start)
    {
        Kind = kind;
        Text = text;
        Value = value;
        Start = start;
    }

    public override string ToString() => Text;
}
