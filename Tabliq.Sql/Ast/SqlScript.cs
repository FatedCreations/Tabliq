using System;
using System.Collections.Generic;
using System.Text;
using Tabliq.Sql.Core;

namespace Tabliq.Sql.Ast;

public class SqlScript : SyntaxNode
{
    public SqlScript(IEnumerable<Statement> statements)
    {
        Statements = new List<Statement>(statements);
    }

    public IReadOnlyList<Statement> Statements { get; } = new List<Statement>();
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var statement in Statements)
        {
            yield return statement;
        }
    }

    public override bool Equals(SyntaxNode? other)
    {
        return other is SqlScript script && Statements.SyntaxSequenceEqual(script.Statements);
    }
}
