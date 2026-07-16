
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/4
/// </summary>
public class Issue4
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH AppStats AS (
                SELECT 
                    ba.[Name] AS Application_Name,
                    ba.[Strategic Status] AS Strategic_Status,
                    COUNT(DISTINCT sa._id) AS Server_Count,
                    COUNT(DISTINCT i._id) AS High_Severity_Incident_Count
                FROM [Business Applications] ba
                JOIN [TR_UB] trub ON ba._id = trub.TR_id
                JOIN [Server Assets] sa ON trub.UB_id = sa._id
                LEFT JOIN [TX_UB] txub ON sa._id = txub.UB_id
                LEFT JOIN [Incidents] i ON txub.TX_id = i._id 
                    AND i.[Priority] IN ('1 - Critical', '2 - High')
                GROUP BY ba.[Name], ba.[Strategic Status]
            ),
            StatusAverages AS (
                SELECT 
                    Strategic_Status,
                    AVG(CAST(High_Severity_Incident_Count AS FLOAT) / NULLIF(Server_Count, 0)) AS Avg_Ratio_For_Status
                FROM AppStats
                GROUP BY Strategic_Status
            )
            SELECT 
                a.Application_Name,
                a.Strategic_Status,
                a.Server_Count,
                a.High_Severity_Incident_Count,
                CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0) AS App_Incident_Ratio,
                s.Avg_Ratio_For_Status,
                (CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0)) - s.Avg_Ratio_For_Status AS Ratio_Deviation
            FROM AppStats a
            JOIN StatusAverages s ON a.Strategic_Status = s.Strategic_Status
            WHERE a.Server_Count > 0 
              AND (CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0)) > s.Avg_Ratio_For_Status
            ORDER BY Ratio_Deviation DESC
            """,
            """
                        WITH AppStats AS (
                SELECT
                    ba.TR_UID AS Application_Name,
                    ba.TR_STR AS Strategic_Status,
                    COUNT(DISTINCT sa.UBId) AS Server_Count,
                    COUNT(DISTINCT i.TXId) AS High_Severity_Incident_Count
                FROM landscapeQuery_strategy_A.TR AS ba
                JOIN landscapeQuery_strategy_A.TR_UB AS trub
                    ON ba.TRId = trub.TRId
                JOIN landscapeQuery_strategy_A.UB AS sa
                    ON trub.UBId = sa.UBId
                LEFT JOIN landscapeQuery_strategy_A.TX_UB AS txub
                    ON sa.UBId = txub.UBId
                LEFT JOIN landscapeQuery_strategy_A.TX AS i
                    ON txub.TXId = i.TXId AND
                    i.TX_PRI IN ('1 - Critical', '2 - High')
                GROUP BY
                    ba.TR_UID,
                    ba.TR_STR
            ), 
            StatusAverages AS (
                SELECT
                    Strategic_Status,
                    AVG(CAST(High_Severity_Incident_Count AS FLOAT) / NULLIF(Server_Count, 0)) AS Avg_Ratio_For_Status
                FROM AppStats
                GROUP BY Strategic_Status
            )
            SELECT
                a.Application_Name,
                a.Strategic_Status,
                a.Server_Count,
                a.High_Severity_Incident_Count,
                CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0) AS App_Incident_Ratio,
                s.Avg_Ratio_For_Status,
                (CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0)) - s.Avg_Ratio_For_Status AS Ratio_Deviation
            FROM AppStats AS a
            JOIN StatusAverages AS s
                ON a.Strategic_Status = s.Strategic_Status
            WHERE
                a.Server_Count > 0 AND
                (CAST(a.High_Severity_Incident_Count AS FLOAT) / NULLIF(a.Server_Count, 0)) > s.Avg_Ratio_For_Status
            ORDER BY Ratio_Deviation DESC
            """);

    [Fact]
    public void TestSimple1()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT 
                *
            FROM [Business Applications] a
            WHERE [Service Impact Ratio] > 1 AND ([Service Impact Ratio] / NULLIF([Service Impact Assumption], 0)) > a.Name
            """,
            """
            SELECT
                a.TRId AS _id,
                a.TR_BUS AS [Business Function],
                a.TR_DES AS Description,
                a.TR_ERV AS [Service Impact Ratio],
                a.TR_INT AS [Internal / Customer Facing],
                a.TR_SER AS [Service Impact Assumption],
                a.TR_STR AS [Strategic Status],
                a.TR_UID AS Name
            FROM landscapeQuery_strategy_A.TR AS a
            WHERE
                a.TR_ERV > 1 AND
                (a.TR_ERV / NULLIF(a.TR_SER, 0)) > a.TR_UID
            """);


    [Fact]
    public void TestSimple2()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Errors(
            """
            SELECT 
                a.[Service Impact Ratio] > 1 as foo
            FROM [Business Applications] a
            WHERE [Service Impact Ratio] > 1 AND ([Service Impact Ratio] / NULLIF([Service Impact Assumption], 0)) > a.Name
            """,
            "UnexpectedCondition: [12:28] : Expected a simple expression but found a condition");
}
