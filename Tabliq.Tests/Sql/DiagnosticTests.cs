using Tabliq.Sql.Diagnostics;

namespace Tabliq.Tests.Sql;

public class DiagnosticTests
{
    [Fact]
    public void UnknownTable()
    {
        var diagnostics = AssertSql
            .WithErrors(
                """
                SELECT * FROM FOO;
                """,
                "TableNotFound: [14:3] : Table 'FOO' not found.");

        var diag = Assert.Single(diagnostics);
        Assert.NotNull(diag.State);
        Assert.Equal("FOO", diag.State?["TableName"]);
    }

    [Fact]
    public void UnknownColumn()
    {
        var diagnostics = AssertSql
            .WithErrors(
                """
                SELECT UnknownColumn FROM [BE];
                """,
                "ColumnNotFound: [7:13] : Column 'UnknownColumn' not found.");

        var diag = Assert.Single(diagnostics);

        Assert.NotNull(diag.State);
        Assert.Null(diag.State["TableName"]);
        Assert.Null(diag.State["SchemaName"]);
        Assert.Equal("UnknownColumn", diag.State["ColumnName"]);
    }

    [Fact]
    public void UnknownColumnForTable()
    {
        var diagnostics = AssertSql
            .WithErrors(
                """
                SELECT BE.UnknownColumn FROM [BE];
                """,
                "ColumnNotFound: [7:16] : Column 'UnknownColumn' not found in table 'BE'.");

        var diag = Assert.Single(diagnostics);

        Assert.NotNull(diag.State);
        Assert.Equal("BE", diag.State["TableName"]);
        Assert.Null(diag.State["SchemaName"]);
        Assert.Equal("UnknownColumn", diag.State["ColumnName"]);
    }
}