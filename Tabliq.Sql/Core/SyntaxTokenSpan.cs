namespace Tabliq.Sql.Core;

public struct SyntaxTokenSpan : IEquatable<SyntaxTokenSpan>
{
    public static SyntaxTokenSpan Empty = default;
    public SyntaxTokenSpan(SyntaxToken startToken, SyntaxToken? endToken = null)
    {
        StartToken = startToken;
        EndToken = endToken ?? startToken;
    }
    public SyntaxTokenSpan(IEnumerable<SyntaxToken> tokens)
        : this(tokens.First(), tokens.Last())
    {
    }

    public SyntaxToken StartToken { get; }
    public SyntaxToken EndToken { get; }
    public int Start => StartToken?.Start ?? 0;
    public int End => EndToken?.End ?? 0;
    public int Length => End - Start;

    public bool Equals(SyntaxTokenSpan other)
    {
        return Start == other.Start && End == other.End;
    }
}