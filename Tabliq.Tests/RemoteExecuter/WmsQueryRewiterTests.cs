namespace Tabliq.Tests.RemoteExecuter;

public class WmsQueryRewiterTests
{
    [Fact]
    public void Issue1()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT [System Name], [Manufacturer], [Model], [EOL / End of Extended Support Date] FROM [Server Assets] WHERE [EOL Status] = 'EOL' OR [EOL / End of Extended Support Date] <= DATEADD(year, 2, GETDATE()) ORDER BY [EOL / End of Extended Support Date] ASC
            """,
            """
            SELECT
                [Server Assets].UB_UID AS [System Name],
                [Server Assets].UB_MAN AS Manufacturer,
                [Server Assets].UB_MOD AS Model,
                [Server Assets].UB_OLE AS [EOL / End of Extended Support Date]
            FROM landscapeQuery_strategy_A.UB AS [Server Assets]
            WHERE
                [Server Assets].UB_EOL = 'EOL' OR
                [Server Assets].UB_OLE <= DATEADD(year, 2, GETDATE())
            ORDER BY [EOL / End of Extended Support Date] ASC
            """);

    [Fact]
    public void Issue2()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT COUNT(*) as RowCount FROM [Server Assets] WHERE [EOL / End of Extended Support Date] IS NOT NULL
            """,
            """
            SELECT COUNT(*) AS RowCount
            FROM landscapeQuery_strategy_A.UB AS [Server Assets]
            WHERE [Server Assets].UB_OLE IS NOT NULL
            """);
    [Fact]
    public void Issue3()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT TOP 50
                ba.[Name] as Application_Name, 
                CASE WHEN SUM( CASE WHEN sa.[EOL Status] IS NOT NULL AND sa.[EOL Status] <> 'Active' THEN 1 ELSE 0 END) * 1.0 / COUNT(sa.[_id]) > 0.5 THEN 'Mostly EOL' ELSE 'Mostly Active' END as EOL_Status,
             COUNT(i.[_id]) as Incident_Count,
             SUM(i.[Business Duration]) as Total_Business_Duration
            FROM [Incidents] i
            JOIN [TX_UB] tx ON i.[_id] = tx.[TX_id]
            JOIN [Server Assets] sa ON tx.[UB_id] = sa.[_id]
            JOIN [TR_UB] tr ON sa.[_id] = tr.[UB_id]
            JOIN [Business Applications] ba ON tr.[TR_id] = ba.[_id]
            GROUP BY ba.[Name]
            ORDER BY Incident_Count DESC
            """,
            """
            SELECT TOP 50
                ba.TR_UID AS Application_Name,
                CASE
                    WHEN SUM(CASE
                        WHEN sa.UB_EOL IS NOT NULL AND
                        sa.UB_EOL <> 'Active' THEN 1
                        ELSE 0
                    END) * 1.0 / COUNT(sa.UBId) > 0.5 THEN 'Mostly EOL'
                    ELSE 'Mostly Active'
                END AS EOL_Status,
                COUNT(i.TXId) AS Incident_Count,
                SUM(i.TX_BUS) AS Total_Business_Duration
            FROM landscapeQuery_strategy_A.TX AS i
            JOIN landscapeQuery_strategy_A.TX_UB AS tx
                ON i.TXId = tx.TXId
            JOIN landscapeQuery_strategy_A.UB AS sa
                ON tx.UBId = sa.UBId
            JOIN landscapeQuery_strategy_A.TR_UB AS tr
                ON sa.UBId = tr.UBId
            JOIN landscapeQuery_strategy_A.TR AS ba
                ON tr.TRId = ba.TRId
            GROUP BY ba.TR_UID
            ORDER BY Incident_Count DESC
            """);

    [Fact]
    public void Issue4()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT '1' where 1 + 1 > 2
            """,
            """
            SELECT '1'
            WHERE 1 + 1 > 2
            """);

    [Fact]
    public void Issue5()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT '1' where 1 + 1 > 2 + 2
            """,
            """
            SELECT '1'
            WHERE 1 + 1 > 2 + 2
            """);

    [Fact]
    public void Issue6()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT '1' where 1 > 2
            """,
            """
            SELECT '1'
            WHERE 1 > 2
            """);
}
