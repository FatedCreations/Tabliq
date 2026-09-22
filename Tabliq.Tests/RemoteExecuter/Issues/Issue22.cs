
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/22
/// </summary>
public class Issue22
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT CAST(NULL AS datetime2(7)) AS min_date FROM [Server Locations] sl
            """,
            """
            
            SELECT CAST(NULL AS datetime2(7)) AS min_date
            FROM landscapeQuery_strategy_A.LO AS sl
            """);
}
