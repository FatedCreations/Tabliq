using Xunit;
using Tabliq.Sql.Rewriter;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Ast;
using System.Linq;

namespace Tabliq.Tests.Sql;

public class Rewiter
{
    [Fact]
    public void ExpandStars()
         => AssertSql
         .WithRewriter<ReplaceStarsRewriter>()
         .Equal(
             """
            SELECT * FROM BE_ES
            """,
            """
            SELECT
                BE_ES.BEId,
                BE_ES.ESId
            FROM BE_ES
            """);

    [Fact]
    public void ExpandStarsAliased()
         => AssertSql
         .WithRewriter<ReplaceStarsRewriter>()
         .Equal(
             """
            SELECT * FROM BE_ES as r
            """,
            """
            SELECT
                r.BEId,
                r.ESId
            FROM BE_ES AS r
            """);
    [Fact]
    public void SkipColumnRewriter()
         => AssertSql
         .WithRewriter<HiddenColumnsRewriter>()
         .Equal(
             """
            SELECT * FROM BE_ES as r
            """,
            """
            SELECT r.BEId
            FROM BE_ES AS r
            """);
    [Fact]
    public void ShouldRewriteTargetedOnly()
         => AssertSql
         .WithRewriter<RewriteTargetedOnly>()
         .Equal(
             """
            SELECT r.*, * FROM BE_ES as r, BE_ES
            """,
            """
            SELECT
                r.BEId,
                r.ESId,
                *
            FROM
                BE_ES AS r,
                BE_ES
            """);

    [Fact]
    public void ShouldRewriteTargetedOnly_PArseOnly()
         => AssertSql
         .Equal(
             """
            SELECT r.*, * FROM BE_ES as r, BE_ES
            """,
            """
            SELECT
                r.*,
                *
            FROM
                BE_ES AS r,
                BE_ES
            """);

    [Fact]
    public void WithAndWithOutAliase()
         => AssertSql
         .WithRewriter<ReplaceStarsRewriter>()
         .Equal(
             """
            SELECT * FROM BE_ES as r, BE_ES
            """,
            """
            SELECT
                r.BEId,
                r.ESId,
                BE_ES.BEId,
                BE_ES.ESId
            FROM
                BE_ES AS r,
                BE_ES
            """);

    private class RewriteTargetedOnly : ReplaceStarsRewriter
    {
        public RewriteTargetedOnly()
        {
        }

        protected override bool ShouldExpand(StarIdentifierExpression binding)
        {

            // By default, expand all columns. Override this method to implement custom logic. hidden expansions etc
            return binding.IdentifierParts.Any();
        }

    }

    private class HiddenColumnsRewriter : ReplaceStarsRewriter
    {
        public HiddenColumnsRewriter()
        {
        }
        protected override bool ShouldExpand(ColumnBinding binding)
        {
            // Hide the ESId column from expansion
            return binding.ColumnSymbol?.Name != "ESId";
        }
    }
}
