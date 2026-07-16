
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/7
/// </summary>
public class Issue7
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT top 100 * FROM [Incidents] WHERE [Automation Status] != '1. No Automation'
            """,
            """
            SELECT TOP 100
                Incidents.TXId AS _id,
                Incidents.TX_ACT AS Active,
                Incidents.TX_ASS AS [Assigned to],
                Incidents.TX_AUT AS [Automation Status],
                Incidents.TX_AVO AS [Avoidable Work Status],
                Incidents.TX_BUS AS [Business Duration],
                Incidents.TX_CLO AS [Close notes],
                Incidents.TX_CRE AS Created,
                Incidents.TX_DUR AS [Duration Category],
                Incidents.TX_ISS AS [Issue Type],
                Incidents.TX_LOG AS [Logged By],
                Incidents.TX_LOS AS [Close Code],
                Incidents.TX_PRI AS Priority,
                Incidents.TX_SHO AS [Short Description],
                Incidents.TX_SSI AS [Assignment Group],
                Incidents.TX_TIC AS [Ticket Status],
                Incidents.TX_UID AS [Incident Reference],
                Incidents.TX_WNA AS [Work Note Complexity],
                Incidents.TX_WOR AS [Work Notes]
            FROM landscapeQuery_strategy_A.TX AS Incidents
            WHERE Incidents.TX_AUT <> '1. No Automation'
            """);
}
