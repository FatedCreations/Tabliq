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
        Assert.Equal(string.Empty, diag.State["TableName"]);
        Assert.Equal(string.Empty, diag.State["SchemaName"]);
        Assert.Equal("UnknownColumn", diag.State["ColumnName"]);
    }
    [Fact]
    public void UnknownColumnBoundTable()
    {
        var diagnostics = AssertSql
            .WithErrors(
                """
                SELECT be.UnknownColumn FROM [BE];
                """,
                "ColumnNotFound: [7:16] : Column 'UnknownColumn' not found in table 'BE'.");

        var diag = Assert.Single(diagnostics);

        Assert.NotNull(diag.State);
        Assert.Equal(string.Empty, diag.State["SchemaName"]);
        Assert.Equal("BE", diag.State["TableName"]);
        Assert.Equal("UnknownColumn", diag.State["ColumnName"]);
    }

    [Fact]
    public void UnknownColumnBoundTableAlias()
    {
        var diagnostics = AssertSql
            .WithErrors(
                """
                SELECT s.UnknownColumn FROM [BE] s;
                """,
                "ColumnNotFound: [7:15] : Column 'UnknownColumn' not found in table 'BE'.");

        var diag = Assert.Single(diagnostics);

        Assert.NotNull(diag.State);
        Assert.Equal(string.Empty, diag.State["SchemaName"]);
        Assert.Equal("BE", diag.State["TableName"]);
        Assert.Equal("s", diag.State["TableNameAlias"]);
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
        Assert.Equal(string.Empty, diag.State["SchemaName"]);
        Assert.Equal("UnknownColumn", diag.State["ColumnName"]);
    }
}