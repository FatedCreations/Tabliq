using Tabliq.Sql.Core;

namespace Tabliq.Sql.Parsing;

internal static class SyntaxNodeExtensions
{
    public static T WithLocation<T>(this T node, SyntaxToken token) where T : SyntaxNode
    {
        node.Span = new SyntaxTokenSpan(token, token);
        return node;
    }
    public static T WithLocation<T>(this T node, Parser.LocationTracker loc) where T : SyntaxNode
    {
        node.Span = loc.Span;
        return node;
    }
    public static T WithLocation<T>(this T node, SyntaxTokenSpan span) where T : SyntaxNode
    {
        node.Span = span;
        return node;
    }
}