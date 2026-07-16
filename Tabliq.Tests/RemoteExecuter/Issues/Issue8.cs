
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/8
/// </summary>
public class Issue8
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH SiteMetrics AS (
                SELECT 
                    SL.[Site Location],
                    SL.[City],
                    SL.[Country Code],
                    COUNT(I.[_id]) AS Incident_Count,
                    AVG(CAST(I.[Business Duration] AS FLOAT)) AS Avg_Business_Duration
                FROM [Server Locations] AS SL
                JOIN [LO_UB] AS LO ON SL.[_id] = LO.[LO_id]
                JOIN [TX_UB] AS TX ON LO.[UB_id] = TX.[UB_id]
                JOIN [Incidents] AS I ON TX.[TX_id] = I.[_id]
                GROUP BY 
                    SL.[Site Location],
                    SL.[City],
                    SL.[Country Code]
            ),
            Thresholds AS (
                SELECT 
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Incident_Count) OVER () AS P75_Count,
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Avg_Business_Duration) OVER () AS P75_Duration
                FROM SiteMetrics
            )
            SELECT DISTINCT
                SM.[Site Location],
                SM.[City],
                SM.[Country Code],
                SM.Incident_Count,
                SM.Avg_Business_Duration
            FROM SiteMetrics AS SM
            CROSS JOIN (SELECT TOP 1 P75_Count, P75_Duration FROM Thresholds) AS T
            WHERE SM.Incident_Count > T.P75_Count
              AND SM.Avg_Business_Duration > T.P75_Duration
            ORDER BY SM.Incident_Count DESC, SM.Avg_Business_Duration DESC
            """,
            """
            WITH SiteMetrics AS (
                SELECT
                    SL.LO_SIT AS [Site Location],
                    SL.LO_CIT AS City,
                    SL.LO_COU AS [Country Code],
                    COUNT(I.TXId) AS Incident_Count,
                    AVG(CAST(I.TX_BUS AS FLOAT)) AS Avg_Business_Duration
                FROM landscapeQuery_strategy_A.LO AS SL
                JOIN landscapeQuery_strategy_A.LO_UB AS LO
                    ON SL.LOId = LO.LOId
                JOIN landscapeQuery_strategy_A.TX_UB AS TX
                    ON LO.UBId = TX.UBId
                JOIN landscapeQuery_strategy_A.TX AS I
                    ON TX.TXId = I.TXId
                GROUP BY
                    SL.LO_SIT,
                    SL.LO_CIT,
                    SL.LO_COU
            ), 
            Thresholds AS (
                SELECT
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Incident_Count) OVER () AS P75_Count,
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Avg_Business_Duration) OVER () AS P75_Duration
                FROM SiteMetrics
            )
            SELECT DISTINCT
                SM.[Site Location],
                SM.City,
                SM.[Country Code],
                SM.Incident_Count,
                SM.Avg_Business_Duration
            FROM SiteMetrics AS SM
            CROSS JOIN (
                SELECT TOP 1
                    P75_Count,
                    P75_Duration
                FROM Thresholds
            ) AS T
            WHERE
                SM.Incident_Count > T.P75_Count AND
                SM.Avg_Business_Duration > T.P75_Duration
            ORDER BY
                SM.Incident_Count DESC,
                SM.Avg_Business_Duration DESC
            """);
    [Fact]
    public void Test2()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH SiteMetrics AS (
                SELECT 
                    SL.[Site Location],
                    SL.[City],
                    SL.[Country Code],
                    COUNT(I.[_id]) AS Incident_Count,
                    AVG(CAST(I.[Business Duration] AS FLOAT)) AS Avg_Business_Duration
                FROM [Server Locations] AS SL
                JOIN [LO_UB] AS LO ON SL.[_id] = LO.[LO_id]
                JOIN [TX_UB] AS TX ON LO.[UB_id] = TX.[UB_id]
                JOIN [Incidents] AS I ON TX.[TX_id] = I.[_id]
                GROUP BY 
                    SL.[Site Location],
                    SL.[City],
                    SL.[Country Code]
            ),
            Thresholds AS (
                SELECT 
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Incident_Count) AS P75_Count,
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Avg_Business_Duration) AS P75_Duration
                FROM SiteMetrics
            )
            SELECT DISTINCT
                SM.[Site Location],
                SM.[City],
                SM.[Country Code],
                SM.Incident_Count,
                SM.Avg_Business_Duration
            FROM SiteMetrics AS SM
            CROSS JOIN (SELECT TOP 1 P75_Count, P75_Duration FROM Thresholds) AS T
            WHERE SM.Incident_Count > T.P75_Count
              AND SM.Avg_Business_Duration > T.P75_Duration
            ORDER BY SM.Incident_Count DESC, SM.Avg_Business_Duration DESC
            """,
            """
            WITH SiteMetrics AS (
                SELECT
                    SL.LO_SIT AS [Site Location],
                    SL.LO_CIT AS City,
                    SL.LO_COU AS [Country Code],
                    COUNT(I.TXId) AS Incident_Count,
                    AVG(CAST(I.TX_BUS AS FLOAT)) AS Avg_Business_Duration
                FROM landscapeQuery_strategy_A.LO AS SL
                JOIN landscapeQuery_strategy_A.LO_UB AS LO
                    ON SL.LOId = LO.LOId
                JOIN landscapeQuery_strategy_A.TX_UB AS TX
                    ON LO.UBId = TX.UBId
                JOIN landscapeQuery_strategy_A.TX AS I
                    ON TX.TXId = I.TXId
                GROUP BY
                    SL.LO_SIT,
                    SL.LO_CIT,
                    SL.LO_COU
            ), 
            Thresholds AS (
                SELECT
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Incident_Count) AS P75_Count,
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Avg_Business_Duration) AS P75_Duration
                FROM SiteMetrics
            )
            SELECT DISTINCT
                SM.[Site Location],
                SM.City,
                SM.[Country Code],
                SM.Incident_Count,
                SM.Avg_Business_Duration
            FROM SiteMetrics AS SM
            CROSS JOIN (
                SELECT TOP 1
                    P75_Count,
                    P75_Duration
                FROM Thresholds
            ) AS T
            WHERE
                SM.Incident_Count > T.P75_Count AND
                SM.Avg_Business_Duration > T.P75_Duration
            ORDER BY
                SM.Incident_Count DESC,
                SM.Avg_Business_Duration DESC
            """);
}
