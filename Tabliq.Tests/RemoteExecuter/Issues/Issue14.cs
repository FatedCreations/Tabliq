
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/14
/// </summary>
public class Issue14
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH asset_app_status AS (
                SELECT
                    tru.UB_id AS asset_id,
                    CASE
                        WHEN SUM(CASE WHEN ba.[Strategic Status] = 'Strategic' THEN 1 ELSE 0 END) > 0 THEN 'Strategic'
                        WHEN COUNT(ba._id) > 0 THEN 'Non-Strategic'
                        ELSE 'Unknown'
                    END AS strategic_application_status
                FROM [TR_UB] tru
                LEFT JOIN [Business Applications] ba
                    ON ba._id = tru.TR_id
                GROUP BY tru.UB_id
            ),
            change_events AS (
                SELECT
                    c._id AS change_id,
                    c.[Change Ref] AS change_ref,
                    c.[Change type] AS change_type,
                    c.[Date applied] AS change_applied_at,
                    eau.UB_id AS asset_id,
                    sa.[Virtualization Technology] AS virtualization_technology,
                    COALESCE(aas.strategic_application_status, 'Unknown') AS strategic_application_status
                FROM [Changes] c
                INNER JOIN [EA_UB] eau
                    ON eau.EA_id = c._id
                INNER JOIN [Server Assets] sa
                    ON sa._id = eau.UB_id
                LEFT JOIN asset_app_status aas
                    ON aas.asset_id = sa._id
            ),
            incident_asset AS (
                SELECT DISTINCT
                    i._id AS incident_id,
                    txu.UB_id AS asset_id,
                    i.[Created] AS incident_created_at,
                    i.[Priority] AS priority_text,
                    CASE
                        WHEN i.[Priority] LIKE '1%' THEN 1
                        WHEN i.[Priority] LIKE '2%' THEN 2
                        WHEN i.[Priority] LIKE '3%' THEN 3
                        WHEN i.[Priority] LIKE '4%' THEN 4
                        WHEN i.[Priority] LIKE '5%' THEN 5
                        ELSE NULL
                    END AS priority_numeric,
                    i.[Business Duration] AS business_duration_minutes
                FROM [Incidents] i
                INNER JOIN [TX_UB] txu
                    ON txu.TX_id = i._id
            ),
            window_defs AS (
                SELECT 1 AS window_days
                UNION ALL SELECT 3
                UNION ALL SELECT 7
            )
            SELECT
                ce.change_id,
                ce.change_ref,
                ce.asset_id,
                ce.change_applied_at,
                ce.change_type,
                ce.virtualization_technology,
                ce.strategic_application_status,
                wd.window_days,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                    THEN ia.incident_id END) AS post_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                    THEN CAST(ia.priority_numeric AS FLOAT) END) AS post_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                    THEN CAST(ia.business_duration_minutes AS FLOAT) END) AS post_avg_business_duration_minutes,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at)
                     AND ia.incident_created_at < ce.change_applied_at
                    THEN ia.incident_id END) AS pre_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at)
                     AND ia.incident_created_at < ce.change_applied_at
                    THEN CAST(ia.priority_numeric AS FLOAT) END) AS pre_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at)
                     AND ia.incident_created_at < ce.change_applied_at
                    THEN CAST(ia.business_duration_minutes AS FLOAT) END) AS pre_avg_business_duration_minutes,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                     AND NOT EXISTS (
                         SELECT 1
                         FROM [EA_UB] eau2
                         INNER JOIN [Changes] c2
                             ON c2._id = eau2.EA_id
                         WHERE eau2.UB_id = ia.asset_id
                           AND c2.[Date applied] >= DATEADD(day, -wd.window_days, ia.incident_created_at)
                           AND c2.[Date applied] < ia.incident_created_at
                     )
                    THEN ia.incident_id END) AS control_same_period_no_recent_change_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                     AND NOT EXISTS (
                         SELECT 1
                         FROM [EA_UB] eau2
                         INNER JOIN [Changes] c2
                             ON c2._id = eau2.EA_id
                         WHERE eau2.UB_id = ia.asset_id
                           AND c2.[Date applied] >= DATEADD(day, -wd.window_days, ia.incident_created_at)
                           AND c2.[Date applied] < ia.incident_created_at
                     )
                    THEN CAST(ia.priority_numeric AS FLOAT) END) AS control_same_period_no_recent_change_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at
                     AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
                     AND NOT EXISTS (
                         SELECT 1
                         FROM [EA_UB] eau2
                         INNER JOIN [Changes] c2
                             ON c2._id = eau2.EA_id
                         WHERE eau2.UB_id = ia.asset_id
                           AND c2.[Date applied] >= DATEADD(day, -wd.window_days, ia.incident_created_at)
                           AND c2.[Date applied] < ia.incident_created_at
                     )
                    THEN CAST(ia.business_duration_minutes AS FLOAT) END) AS control_same_period_no_recent_change_avg_business_duration_minutes
            FROM change_events ce
            CROSS JOIN window_defs wd
            LEFT JOIN incident_asset ia
                ON ia.asset_id = ce.asset_id
               AND ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at)
               AND ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
            GROUP BY
                ce.change_id,
                ce.change_ref,
                ce.asset_id,
                ce.change_applied_at,
                ce.change_type,
                ce.virtualization_technology,
                ce.strategic_application_status,
                wd.window_days
            ORDER BY ce.change_applied_at, ce.change_id, wd.window_days;
            """,
            """
            WITH asset_app_status AS (
                SELECT
                    tru.UBId AS asset_id,
                    CASE
                        WHEN SUM(CASE
                            WHEN ba.TR_STR = 'Strategic' THEN 1
                            ELSE 0
                        END) > 0 THEN 'Strategic'
                        WHEN COUNT(ba.TRId) > 0 THEN 'Non-Strategic'
                        ELSE 'Unknown'
                    END AS strategic_application_status
                FROM landscapeQuery_strategy_A.TR_UB AS tru
                LEFT JOIN landscapeQuery_strategy_A.TR AS ba
                    ON ba.TRId = tru.TRId
                GROUP BY tru.UBId
            ), 
            change_events AS (
                SELECT
                    c.EAId AS change_id,
                    c.EA_UID AS change_ref,
                    c.EA_CHA AS change_type,
                    c.EA_DAT AS change_applied_at,
                    eau.UBId AS asset_id,
                    sa.UB_IRT AS virtualization_technology,
                    COALESCE(aas.strategic_application_status, 'Unknown') AS strategic_application_status
                FROM landscapeQuery_strategy_A.EA AS c
                INNER JOIN landscapeQuery_strategy_A.EA_UB AS eau
                    ON eau.EAId = c.EAId
                INNER JOIN landscapeQuery_strategy_A.UB AS sa
                    ON sa.UBId = eau.UBId
                LEFT JOIN asset_app_status AS aas
                    ON aas.asset_id = sa.UBId
            ), 
            incident_asset AS (
                SELECT DISTINCT
                    i.TXId AS incident_id,
                    txu.UBId AS asset_id,
                    i.TX_CRE AS incident_created_at,
                    i.TX_PRI AS priority_text,
                    CASE
                        WHEN i.TX_PRI LIKE '1%' THEN 1
                        WHEN i.TX_PRI LIKE '2%' THEN 2
                        WHEN i.TX_PRI LIKE '3%' THEN 3
                        WHEN i.TX_PRI LIKE '4%' THEN 4
                        WHEN i.TX_PRI LIKE '5%' THEN 5
                        ELSE NULL
                    END AS priority_numeric,
                    i.TX_BUS AS business_duration_minutes
                FROM landscapeQuery_strategy_A.TX AS i
                INNER JOIN landscapeQuery_strategy_A.TX_UB AS txu
                    ON txu.TXId = i.TXId
            ), 
            window_defs AS (
                SELECT 1 AS window_days
                UNION ALL
                SELECT 3
                UNION ALL
                SELECT 7
            )
            SELECT
                ce.change_id,
                ce.change_ref,
                ce.asset_id,
                ce.change_applied_at,
                ce.change_type,
                ce.virtualization_technology,
                ce.strategic_application_status,
                wd.window_days,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) THEN ia.incident_id
                END) AS post_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) THEN CAST(ia.priority_numeric AS FLOAT)
                END) AS post_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) THEN CAST(ia.business_duration_minutes AS FLOAT)
                END) AS post_avg_business_duration_minutes,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at) AND
                    ia.incident_created_at < ce.change_applied_at THEN ia.incident_id
                END) AS pre_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at) AND
                    ia.incident_created_at < ce.change_applied_at THEN CAST(ia.priority_numeric AS FLOAT)
                END) AS pre_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at) AND
                    ia.incident_created_at < ce.change_applied_at THEN CAST(ia.business_duration_minutes AS FLOAT)
                END) AS pre_avg_business_duration_minutes,
                COUNT(DISTINCT CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) AND
                    NOT EXISTS (
                        SELECT 1
                        FROM landscapeQuery_strategy_A.EA_UB AS eau2
                        INNER JOIN landscapeQuery_strategy_A.EA AS c2
                            ON c2.EAId = eau2.EAId
                        WHERE
                            eau2.UBId = ia.asset_id AND
                            c2.EA_DAT >= DATEADD(day, -wd.window_days, ia.incident_created_at) AND
                            c2.EA_DAT < ia.incident_created_at
                    ) THEN ia.incident_id
                END) AS control_same_period_no_recent_change_incident_count,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) AND
                    NOT EXISTS (
                        SELECT 1
                        FROM landscapeQuery_strategy_A.EA_UB AS eau2
                        INNER JOIN landscapeQuery_strategy_A.EA AS c2
                            ON c2.EAId = eau2.EAId
                        WHERE
                            eau2.UBId = ia.asset_id AND
                            c2.EA_DAT >= DATEADD(day, -wd.window_days, ia.incident_created_at) AND
                            c2.EA_DAT < ia.incident_created_at
                    ) THEN CAST(ia.priority_numeric AS FLOAT)
                END) AS control_same_period_no_recent_change_avg_priority_numeric,
                AVG(CASE
                    WHEN ia.incident_created_at >= ce.change_applied_at AND
                    ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at) AND
                    NOT EXISTS (
                        SELECT 1
                        FROM landscapeQuery_strategy_A.EA_UB AS eau2
                        INNER JOIN landscapeQuery_strategy_A.EA AS c2
                            ON c2.EAId = eau2.EAId
                        WHERE
                            eau2.UBId = ia.asset_id AND
                            c2.EA_DAT >= DATEADD(day, -wd.window_days, ia.incident_created_at) AND
                            c2.EA_DAT < ia.incident_created_at
                    ) THEN CAST(ia.business_duration_minutes AS FLOAT)
                END) AS control_same_period_no_recent_change_avg_business_duration_minutes
            FROM change_events AS ce
            CROSS JOIN window_defs AS wd
            LEFT JOIN incident_asset AS ia
                ON ia.asset_id = ce.asset_id AND
                ia.incident_created_at >= DATEADD(day, -wd.window_days, ce.change_applied_at) AND
                ia.incident_created_at < DATEADD(day, wd.window_days, ce.change_applied_at)
            GROUP BY
                ce.change_id,
                ce.change_ref,
                ce.asset_id,
                ce.change_applied_at,
                ce.change_type,
                ce.virtualization_technology,
                ce.strategic_application_status,
                wd.window_days
            ORDER BY
                ce.change_applied_at,
                ce.change_id,
                wd.window_days
            """);

    [Fact]
    public void Minimal()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
                SELECT
                    -c.[Date applied] AS change_applied_at
                FROM [Changes] c
            """,
            """
            SELECT -c.EA_DAT AS change_applied_at
            FROM landscapeQuery_strategy_A.EA AS c
            """);
}
