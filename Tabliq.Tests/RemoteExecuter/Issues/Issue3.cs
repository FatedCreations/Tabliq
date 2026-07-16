namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/3
/// </summary>
public class Issue3
{
    [Fact]
    public void InvalidSql()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Errors(
            """
            SELECT 'Incidents' as tbl, [Automation Status], [Business Duration], [Work Notes] FROM [Incidents] WHERE [Automation Status] IS NOT NULL ORDER BY 1 LIMIT 5
            """,
            "UnexpectedToken: [148:7] : 'LIMIT' was unexpected");

    // rewitten alternative syntax
    [Fact]
    public void AlternativeSyntax()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT 'Incidents' as tbl, [Automation Status], [Business Duration], [Work Notes] FROM [Incidents] WHERE [Automation Status] IS NOT NULL ORDER BY 1 OFFSET 0 FETCH 5
            """,
            """
            SELECT
                'Incidents' AS tbl,
                Incidents.TX_AUT AS [Automation Status],
                Incidents.TX_BUS AS [Business Duration],
                Incidents.TX_WOR AS [Work Notes]
            FROM landscapeQuery_strategy_A.TX AS Incidents
            WHERE Incidents.TX_AUT IS NOT NULL
            ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY
            """);
}
