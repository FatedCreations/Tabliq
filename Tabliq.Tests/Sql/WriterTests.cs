using Tabliq.Sql.Ast;
using Tabliq.Sql.Core;
using Tabliq.Sql.Diagnostics;
using Tabliq.Sql.Parsing;
using Tabliq.Sql.Printer;
using Xunit;

namespace Tabliq.Tests.Sql;

public class WriterTests
{

    [Fact]
    public void ParseSelectSql()
    {
        var tree = Parser.Parse("SELECT Id FROM Users WHERE a = b");
        Assert.Equal(
            """
            SELECT Id
            FROM Users
            WHERE a = b
            """,
            new SqlWriter().ToSql(tree.Script));
    }

    [Fact]
    public void ParseSelectSqlMultLineWhereClause()
    {
        var tree = Parser.Parse("SELECT Id FROM Users WHERE (a = b AND cc < dd)");
        Assert.Equal(
            """
            SELECT Id
            FROM Users
            WHERE (
                a = b AND
                cc < dd
            )
            """,
            new SqlWriter().ToSql(tree.Script));
    }
}

