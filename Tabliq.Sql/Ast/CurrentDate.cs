using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class CurrentTime : Expression
{
    public CurrentTime()
    {
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is CurrentTime otherLiteral;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}
public class CurrentDate : Expression
{
    public CurrentDate()
    {
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is CurrentDate otherLiteral;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public class CurrentTimestamp : Expression
{
    public CurrentTimestamp()
    {
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is CurrentTimestamp otherLiteral;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}

public class NullValue : Expression
{
    public NullValue()
    {
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is NullValue otherLiteral;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield break;
    }
}
