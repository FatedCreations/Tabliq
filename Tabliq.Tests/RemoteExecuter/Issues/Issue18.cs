
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/15
/// </summary>
public class Issue18
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT
                lo."UB_id" AS asset_id
            FROM "LO_UB" lo
            ORDER BY lo."UB_id"  DESC
            FETCH FIRST 10 ROWS ONLY
            """,
            """
            SELECT lo.UBId AS asset_id
            FROM landscapeQuery_strategy_A.LO_UB AS lo
            ORDER BY lo.UBId DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
            """);
}
