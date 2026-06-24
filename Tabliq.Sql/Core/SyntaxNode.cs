using System.Collections.Generic;
using System.Diagnostics;
using Tabliq.Sql.Printer;

namespace Tabliq.Sql.Core;

[DebuggerDisplay("{DebugString()}")]
public abstract class SyntaxNode : IEquatable<SyntaxNode>
{
    public SyntaxTokenSpan Span { get; set; }

    public abstract IEnumerable<SyntaxNode> GetChildren();

    public abstract bool Equals(SyntaxNode? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is SyntaxNode n)
        {
            return Equals(n);
        }

        return false;
    }

    private string DebugString()
    {
        return $"{this.GetType().Name}: {new SqlWriter().ToSql(this)}";
    }

    public override string ToString()
    {
        return new SqlWriter().ToSql(this);
    }
}

internal static class SyntaxNodeExtensions
{
    public static bool SyntaxSequenceEqual(this IEnumerable<SyntaxNode> first, IEnumerable<SyntaxNode> second)
    {
        using var enumerator1 = first.GetEnumerator();
        using var enumerator2 = second.GetEnumerator();
        while (true)
        {
            var hasNext1 = enumerator1.MoveNext();
            var hasNext2 = enumerator2.MoveNext();
            if (!hasNext1 && !hasNext2)
                return true;
            if (hasNext1 != hasNext2)
                return false;
            if (!enumerator1.Current.Equals(enumerator2.Current))
                return false;
        }
    }
}
