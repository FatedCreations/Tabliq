using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Core;
using Xunit;

namespace Tabliq.Tests.Sql;

public class BinderTests
{
    // binder testing can be validated using rewriter!
    [Fact]
    public void BindSelect()
        => AssertSql.Equal(
            $"""
            SELECT BEId
            FROM BE
            """,
            new SqlScript([
                new SelectStatement([],
                    new SelectExpression(
                            false,
                            null,
                            Distinctness.Unspecified,
                            [
                                new SelectProjection(
                                    new IdentifierExpression("BEId"),
                                    "BEId",
                                    true
                                )
                            ],
                            new FromClause(
                                [
                                    new NamedTableReference(new IdentifierExpression("BE"), null)
                                ],
                                []
                            ),
                            null,
                            null,
                            null,
                            null,
                            []
                        )
                    ),
                ]
            ));
}

