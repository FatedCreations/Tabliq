namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/2
/// </summary>
public class Issue2
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH ChangeEvents AS (
                SELECT 
                    [Change type], 
                    [Date applied] AS change_time,
                    [Changes]._id AS change_id
                FROM [Changes]
            ),
            IncidentCounts AS (
                SELECT 
                    ce.[Change type],
                    ce.change_time,
                    ce.change_id,
                    (
                        SELECT COUNT(*) 
                        FROM [Incidents] i 
                        WHERE i.[Created] >= ce.change_time 
                          AND i.[Created] <= DATEADD(hour, 48, ce.change_time)
                    ) AS incidents_post_change_48h
                FROM ChangeEvents ce
            ),
            Baseline AS (
                SELECT 
                    COUNT(*) / NULLIF(DATEDIFF(day, MIN([Created]), MAX([Created])), 0) AS avg_incidents_per_day
                FROM [Incidents]
            )
            SELECT 
                ic.[Change type],
                COUNT(ic.change_id) AS total_changes,
                AVG(CAST(ic.incidents_post_change_48h AS FLOAT)) AS avg_incidents_within_48h,
                MAX(ic.incidents_post_change_48h) AS max_incidents_within_48h,
                (SELECT avg_incidents_per_day * 2 FROM Baseline) AS baseline_48h_incidents
            FROM IncidentCounts ic
            GROUP BY ic.[Change type]
            ORDER BY avg_incidents_within_48h DESC
            """,
            """
            WITH ChangeEvents AS (
                SELECT
                    Changes.EA_CHA AS [Change type],
                    Changes.EA_DAT AS change_time,
                    Changes.EAId AS change_id
                FROM landscapeQuery_strategy_A.EA AS Changes
            ), 
            IncidentCounts AS (
                SELECT
                    ce.[Change type],
                    ce.change_time,
                    ce.change_id,
                    (
                        SELECT COUNT(*)
                        FROM landscapeQuery_strategy_A.TX AS i
                        WHERE
                            i.TX_CRE >= ce.change_time AND
                            i.TX_CRE <= DATEADD(hour, 48, ce.change_time)
                    ) AS incidents_post_change_48h
                FROM ChangeEvents AS ce
            ), 
            Baseline AS (
                SELECT COUNT(*) / NULLIF(DATEDIFF(day, MIN(Incidents.TX_CRE), MAX(Incidents.TX_CRE)), 0) AS avg_incidents_per_day
                FROM landscapeQuery_strategy_A.TX AS Incidents
            )
            SELECT
                ic.[Change type],
                COUNT(ic.change_id) AS total_changes,
                AVG(CAST(ic.incidents_post_change_48h AS FLOAT)) AS avg_incidents_within_48h,
                MAX(ic.incidents_post_change_48h) AS max_incidents_within_48h,
                (
                    SELECT avg_incidents_per_day * 2
                    FROM Baseline
                ) AS baseline_48h_incidents
            FROM IncidentCounts AS ic
            GROUP BY ic.[Change type]
            ORDER BY avg_incidents_within_48h DESC
            """);

}
