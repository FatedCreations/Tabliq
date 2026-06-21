using Xunit;
using Tabliq.Sql.Core;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Parsing;

namespace Tabliq.Tests.Sql;

public class ParserTests
{
    [Fact]
    public void ParseSelect()
    {
        var tree = Parser.Parse("SELECT Id FROM Users");
        Assert.NotEmpty(tree.Script.Statements);
    }

    [Fact]
    public void ParseSelectAst()
    {
        var tree = Parser.Parse("SELECT Id FROM Users WHERE a = b");
        var ast = new SqlScript([
            new SelectStatement([],
                new SelectExpression(
                        false,
                        null,
                        Distinctness.Unspecified,
                        [
                            new SelectProjection(
                                new IdentifierExpression("Id"),
                                "Id",
                                true
                            )
                        ],
                        new FromClause(
                            [
                                new NamedTableReference(new IdentifierExpression("Users"), null)
                            ],
                            []
                        ),
                        new WhereClause(
                            new BinaryComparisonCondition(
                                new IdentifierExpression("a"),
                                BinaryCompararisonOperator.Equals,
                                new IdentifierExpression("b")
                            )
                        ),
                        null, 
                        null,
                        null,
                        []
                    )),
        ]);
        Assert.Equal(ast, tree.Script);
    }
}


