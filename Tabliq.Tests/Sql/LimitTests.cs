using Tabliq.Sql;
using Tabliq.Sql.Parsing;

namespace Tabliq.Tests.Sql;

public class LimitTests
{
    private const string Table = "[TAC SRs]";

    private const string GroupColumn = "Incident Status";

    private static readonly string OneColumnSql = $"SELECT [{GroupColumn}] FROM {Table}";

    [Theory]
    [InlineData("not", "AND, OR, NOT and UNION operators")]
    [InlineData("or", "AND, OR, NOT and UNION operators")]
    public async Task ValidateDefaultConditionLimits(string shape, string reason)
    {
        // Each of these overflows the parser's stack, which ends the process instead of throwing, so the
        // refusal has to come before the parser and both endpoints have to apply it.
        var sql = Nested(shape, 5_000);

        var results = Parser.Parse(sql);

        Assert.Contains("ConditionDepthExceeded", results.Diagnostics.Select(x => x.Id));
    }

    [Theory]
    [InlineData("comparisons", "UnexpectedToken: '<' was unexpected (52,19995)")]
    public async Task Invalid(string shape, string expectedError)
    {
        // Each of these overflows the parser's stack, which ends the process instead of throwing, so the
        // refusal has to come before the parser and both endpoints have to apply it.
        var sql = Nested(shape, 5_000);

        var results = Parser.Parse(sql);

        var msg = Assert.Single(results.Diagnostics.Select(x => x.ToString()));
        Assert.Equal(expectedError, msg);
    }


    [Theory]
    [InlineData("case", "levels of brackets or CASE")]
    [InlineData("minus", "arithmetic operators")]
    [InlineData("end-in-brackets", "levels of brackets or CASE")]
    public async Task ValidateDefaultExpressionLiimits(string shape, string reason)
    {
        // Each of these overflows the parser's stack, which ends the process instead of throwing, so the
        // refusal has to come before the parser and both endpoints have to apply it.
        var sql = Nested(shape, 5_000);

        var results = Parser.Parse(sql);

        Assert.Contains("ExpressionDepthExceeded", results.Diagnostics.Select(x => x.Id));
    }


    [Theory]
    [InlineData("subqueries", "levels of brackets or CASE")]
    [InlineData("union", "AND, OR, NOT and UNION operators")]
    [InlineData("stray-closers", "levels of brackets or CASE")]
    public async Task ValidateDefaultQueryDepthLimit(string shape, string reason)
    {
        // Each of these overflows the parser's stack, which ends the process instead of throwing, so the
        // refusal has to come before the parser and both endpoints have to apply it.
        var sql = Nested(shape, 5_000);
        
        var results = Parser.Parse(sql);

        Assert.Contains("QueryDepthExceeded", results.Diagnostics.Select(x => x.Id));
    }

    [Fact]
    public void just_missing_limit_shoudl_error()
    {
        var defualts = new TabliqSettings();
        //defualts.MaxSubQueryDepth = 5;
        //defualts.MaxConditionDepth = 4;
        // defualts.MaxConditionDepth = 5;
        // Subqueries to the depth limit, with the logical and arithmetic operators all spent in the
        // innermost one: the most stack a query the guard accepts can ask of parsing, binding and rewriting.
        var sqlDepth = defualts.MaxSubQueryDepth ?? 0;
        var expDepth = defualts.MaxExpressionDepth ?? 0;
        var conDepth = defualts.MaxConditionDepth ?? 0;

        var sqlRepeat = sqlDepth + 1;

        var sql = Nested("subqueries", sqlRepeat);

        var results = Parser.Parse(sql, defualts);
        Assert.Contains("QueryDepthExceeded", results.Diagnostics.Select(x => x.Id));
    }

    [Fact]
    public void A_query_at_every_limit_at_once_still_runs()
    {
        var defualts = new TabliqSettings();
        //defualts.MaxSubQueryDepth = 5;
        //defualts.MaxConditionDepth = 4;
        // defualts.MaxConditionDepth = 5;
        // Subqueries to the depth limit, with the logical and arithmetic operators all spent in the
        // innermost one: the most stack a query the guard accepts can ask of parsing, binding and rewriting.
        var sqlDepth = defualts.MaxSubQueryDepth ?? 0;
        var expDepth = defualts.MaxExpressionDepth ?? 0;
        var conDepth = defualts.MaxConditionDepth ?? 0;

        var sqlRepeat = sqlDepth - 3;
        var expRepeat = expDepth - 3;
        var conRepeat = conDepth - 3;

        var sql =
            $"SELECT [{GroupColumn}] FROM " +
            string.Concat(Enumerable.Repeat($"(SELECT [{GroupColumn}] FROM ", sqlRepeat)) +
            $"(SELECT [{GroupColumn}], {string.Concat(Enumerable.Repeat("- ", expRepeat))}1 AS n FROM {Table} " +
            $"WHERE {string.Concat(Enumerable.Repeat("NOT ", conRepeat))}[{GroupColumn}] = 'a') x" +
            string.Concat(Enumerable.Repeat(") x", sqlRepeat));

        var results = Parser.Parse(sql, defualts);
        results.ThrowIfInvalid();
        Assert.Empty(results.Diagnostics);
        Assert.NotNull(results.Script);
    }

    [Fact]
    public void A_query_at_every_limit_at_once_parses_on_half_a_megabyte_of_stack()
    {
        var thread = new Thread(() => A_query_at_every_limit_at_once_still_runs(), 512 * 1024);
        thread.Start();
        thread.Join();
    }

    private static string Nested(string shape, int n) => shape switch
    {
        "subqueries" => $"SELECT [{GroupColumn}] FROM " + string.Concat(Enumerable.Repeat($"(SELECT [{GroupColumn}] FROM ", n)) + Table + string.Concat(Enumerable.Repeat(") x", n)),
        "case" => "SELECT " + string.Concat(Enumerable.Repeat("CASE WHEN 1 = 1 THEN ", n)) + "1" + string.Concat(Enumerable.Repeat(" END", n)) + $" FROM {Table}",
        "not" => $"{OneColumnSql} WHERE " + string.Concat(Enumerable.Repeat("NOT ", n)) + $"[{GroupColumn}] = 'a'",
        "or" => $"{OneColumnSql} WHERE " + string.Join(" OR ", Enumerable.Range(0, n).Select(i => $"[{GroupColumn}] = '{i}'")),
        "union" => string.Join(" UNION ", Enumerable.Repeat(OneColumnSql, n)),
        "minus" => "SELECT " + string.Concat(Enumerable.Repeat("- ", n)) + $"1 FROM {Table}",
        "comparisons" => $"{OneColumnSql} WHERE 1" + string.Concat(Enumerable.Repeat(" < 1", n)),
        // A closer only cancels an opener of its own kind: neither a run of ')' up front nor an END
        // inside each bracket lowers the depth the parser actually reaches.
        "stray-closers" => $"{OneColumnSql} " + new string(')', n) + $" WHERE [{GroupColumn}] IN " + string.Concat(Enumerable.Repeat($"(SELECT [{GroupColumn}] FROM {Table} WHERE [{GroupColumn}] IN ", n)) + "('a')" + new string(')', n),
        "end-in-brackets" => "SELECT " + string.Concat(Enumerable.Repeat("(END ", n)) + "1" + new string(')', n) + $" FROM {Table}",
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };



    [Fact]
    public void SubQueryLimits()
        => AssertSql
            .WithSettings(s => s.MaxSubQueryDepth = 3)
            .WithErrors(
                """
                SELECT Count(*)
                FROM 
                (
                    SELECT *
                    FROM
                    (
                        SELECT *
                        FROM
                        (
                            SELECT *
                            FROM
                            (
                                SELECT *
                                FROM BE
                            ) d
                        ) d
                    ) d
                ) d;
                """,
                "QueryDepthExceeded: [60:0] : Query depth exceeded the maximum allowed depth of 3.");
    [Fact]
    public void LimitTokenCount()
        => AssertSql
            .WithSettings(s => s.MaxTokenLimit = 4)
            .WithErrors(
                """
                SELECT Count(*) FROM BE;
                """,
                "MaxTokenLimitExceeded: [14:0] : Maximum token limit of 4 exceeded.");


    [Fact]
    public void ExpressionDepth()
        => AssertSql
            .WithSettings(s => s.MaxExpressionDepth = 3)
            .WithErrors(
                """
                SELECT SUM( 1 + 2 + 3 + 4 + 5) FROM BE;
                """,
                "ExpressionDepthExceeded: [12:0] : Expression depth exceeded the maximum allowed depth of 3.");

    [Fact]
    public void ConditionLimit()
        => AssertSql
            .WithSettings(s => s.MaxConditionDepth = 3)
            .WithErrors(
                """
                SELECT count(*) FROM BE where 1 = 1 AND 2 = 2 AND 3 = 3 AND 4 = 4;
                """,
                "ConditionDepthExceeded: [30:0] : Condition depth exceeded the maximum allowed depth of 3.");
}