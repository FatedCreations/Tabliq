
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/15
/// </summary>
public class Issue15
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
               WITH windows AS (
                    SELECT 1 AS window_days
                    UNION ALL SELECT 3
                    UNION ALL SELECT 7
                ),
                change_assets AS (
                    SELECT
                        c._id AS change_id,
                        e.UB_id AS asset_id,
                        c.[Change type] AS change_type,
                        c.[Date applied] AS change_date,
                        sa.[Virtualization Technology] AS virtualization_technology,
                        COALESCE(apps.strategic_status, 'Unknown') AS strategic_status
                    FROM [Changes] c
                    JOIN [EA_UB] e
                        ON e.EA_id = c._id
                    LEFT JOIN [Server Assets] sa
                        ON sa._id = e.UB_id
                    LEFT JOIN (
                        SELECT
                            t.UB_id AS asset_id,
                            CASE
                                WHEN MAX(CASE WHEN ba.[Strategic Status] IS NOT NULL THEN 1 ELSE 0 END) = 0 THEN 'Unknown'
                                WHEN MAX(CASE WHEN ba.[Strategic Status] IN ('Strategic','Core','Core Enterprise Application','Yes','True') THEN 1 ELSE 0 END) = 1 THEN 'Strategic'
                                ELSE 'Non-Strategic'
                            END AS strategic_status
                        FROM [TR_UB] t
                        LEFT JOIN [Business Applications] ba
                            ON ba._id = t.TR_id
                        GROUP BY t.UB_id
                    ) apps
                        ON apps.asset_id = e.UB_id
                ),
                asset_incidents AS (
                    SELECT
                        x.UB_id AS asset_id,
                        i._id AS incident_id,
                        i.[Created] AS incident_created,
                        i.[Business Duration] AS business_duration,
                        CASE
                            WHEN i.[Priority] LIKE '1%' THEN 1
                            WHEN i.[Priority] LIKE '2%' THEN 2
                            WHEN i.[Priority] LIKE '3%' THEN 3
                            WHEN i.[Priority] LIKE '4%' THEN 4
                            WHEN i.[Priority] LIKE '5%' THEN 5
                            ELSE NULL
                        END AS priority_num
                    FROM [TX_UB] x
                    JOIN [Incidents] i
                        ON i._id = x.TX_id
                ),
                change_windows AS (
                    SELECT
                        ca.change_id,
                        ca.asset_id,
                        ca.change_type,
                        ca.change_date,
                        ca.virtualization_technology,
                        ca.strategic_status,
                        w.window_days,
                        (0 - w.window_days) AS neg_window_days
                    FROM change_assets ca
                    CROSS JOIN windows w
                ),
                changed_asset_metrics AS (
                    SELECT
                        cw.window_days,
                        cw.change_type,
                        cw.virtualization_technology,
                        cw.strategic_status,
                        cw.change_id,
                        cw.asset_id,
                        COUNT(CASE
                            WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date)
                             AND ai.incident_created < cw.change_date
                            THEN 1 END) AS pre_incident_count,
                        COUNT(CASE
                            WHEN ai.incident_created >= cw.change_date
                             AND ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                            THEN 1 END) AS post_incident_count,
                        AVG(CASE
                            WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date)
                             AND ai.incident_created < cw.change_date
                            THEN CAST(ai.priority_num AS FLOAT) END) AS pre_priority_avg,
                        AVG(CASE
                            WHEN ai.incident_created >= cw.change_date
                             AND ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                            THEN CAST(ai.priority_num AS FLOAT) END) AS post_priority_avg,
                        AVG(CASE
                            WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date)
                             AND ai.incident_created < cw.change_date
                            THEN CAST(ai.business_duration AS FLOAT) END) AS pre_business_duration_avg,
                        AVG(CASE
                            WHEN ai.incident_created >= cw.change_date
                             AND ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                            THEN CAST(ai.business_duration AS FLOAT) END) AS post_business_duration_avg
                    FROM change_windows cw
                    LEFT JOIN asset_incidents ai
                        ON ai.asset_id = cw.asset_id
                       AND ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date)
                       AND ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                    GROUP BY
                        cw.window_days,
                        cw.change_type,
                        cw.virtualization_technology,
                        cw.strategic_status,
                        cw.change_id,
                        cw.asset_id
                ),
                control_asset_metrics AS (
                    SELECT
                        cw.window_days,
                        cw.change_type,
                        cw.virtualization_technology,
                        cw.strategic_status,
                        cw.change_id,
                        COUNT(DISTINCT ai.incident_id) AS control_post_incident_count,
                        AVG(CAST(ai.priority_num AS FLOAT)) AS control_post_priority_avg,
                        AVG(CAST(ai.business_duration AS FLOAT)) AS control_post_business_duration_avg
                    FROM change_windows cw
                    JOIN [Server Assets] sa2
                        ON sa2._id <> cw.asset_id
                    LEFT JOIN [EA_UB] e2
                        ON e2.UB_id = sa2._id
                    LEFT JOIN [Changes] c2
                        ON c2._id = e2.EA_id
                       AND c2.[Date applied] >= DATEADD(day, cw.neg_window_days, cw.change_date)
                       AND c2.[Date applied] < DATEADD(day, cw.window_days, cw.change_date)
                    LEFT JOIN asset_incidents ai
                        ON ai.asset_id = sa2._id
                       AND ai.incident_created >= cw.change_date
                       AND ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                    WHERE c2._id IS NULL
                    GROUP BY
                        cw.window_days,
                        cw.change_type,
                        cw.virtualization_technology,
                        cw.strategic_status,
                        cw.change_id
                )
                SELECT
                    cam.window_days,
                    cam.change_type,
                    cam.virtualization_technology,
                    cam.strategic_status,
                    COUNT(*) AS changed_asset_change_events,
                    AVG(CAST(cam.post_incident_count AS FLOAT)) AS mean_post_incident_count,
                    VAR_SAMP(CAST(cam.post_incident_count AS FLOAT)) AS var_post_incident_count,
                    AVG(CAST(cam.pre_incident_count AS FLOAT)) AS mean_pre_incident_count,
                    VAR_SAMP(CAST(cam.pre_incident_count AS FLOAT)) AS var_pre_incident_count,
                    AVG(cam.post_priority_avg) AS mean_post_priority,
                    VAR_SAMP(cam.post_priority_avg) AS var_post_priority,
                    AVG(cam.pre_priority_avg) AS mean_pre_priority,
                    VAR_SAMP(cam.pre_priority_avg) AS var_pre_priority,
                    AVG(cam.post_business_duration_avg) AS mean_post_business_duration,
                    VAR_SAMP(cam.post_business_duration_avg) AS var_post_business_duration,
                    AVG(cam.pre_business_duration_avg) AS mean_pre_business_duration,
                    VAR_SAMP(cam.pre_business_duration_avg) AS var_pre_business_duration,
                    AVG(CAST(cam.post_incident_count - cam.pre_incident_count AS FLOAT)) AS mean_incident_count_delta,
                    AVG(cam.post_priority_avg - cam.pre_priority_avg) AS mean_priority_delta,
                    AVG(cam.post_business_duration_avg - cam.pre_business_duration_avg) AS mean_business_duration_delta,
                    AVG(CAST(ctrl.control_post_incident_count AS FLOAT)) AS mean_control_post_incident_count,
                    VAR_SAMP(CAST(ctrl.control_post_incident_count AS FLOAT)) AS var_control_post_incident_count,
                    AVG(ctrl.control_post_priority_avg) AS mean_control_post_priority,
                    VAR_SAMP(ctrl.control_post_priority_avg) AS var_control_post_priority,
                    AVG(ctrl.control_post_business_duration_avg) AS mean_control_post_business_duration,
                    VAR_SAMP(ctrl.control_post_business_duration_avg) AS var_control_post_business_duration
                FROM changed_asset_metrics cam
                LEFT JOIN control_asset_metrics ctrl
                    ON ctrl.change_id = cam.change_id
                   AND ctrl.window_days = cam.window_days
                   AND ctrl.change_type = cam.change_type
                   AND ctrl.virtualization_technology = cam.virtualization_technology
                   AND ctrl.strategic_status = cam.strategic_status
                GROUP BY
                    cam.window_days,
                    cam.change_type,
                    cam.virtualization_technology,
                    cam.strategic_status
                ORDER BY
                    cam.window_days,
                    changed_asset_change_events DESC,
                    cam.change_type,
                    cam.virtualization_technology,
                    cam.strategic_status;
            """,
            """
            WITH windows AS (
                SELECT 1 AS window_days
                UNION ALL
                SELECT 3
                UNION ALL
                SELECT 7
            ), 
            change_assets AS (
                SELECT
                    c.EAId AS change_id,
                    e.UBId AS asset_id,
                    c.EA_CHA AS change_type,
                    c.EA_DAT AS change_date,
                    sa.UB_IRT AS virtualization_technology,
                    COALESCE(apps.strategic_status, 'Unknown') AS strategic_status
                FROM landscapeQuery_strategy_A.EA AS c
                JOIN landscapeQuery_strategy_A.EA_UB AS e
                    ON e.EAId = c.EAId
                LEFT JOIN landscapeQuery_strategy_A.UB AS sa
                    ON sa.UBId = e.UBId
                LEFT JOIN (
                    SELECT
                        t.UB_id AS asset_id,
                        CASE
                            WHEN MAX(CASE
                                WHEN ba.[Strategic Status] IS NOT NULL THEN 1
                                ELSE 0
                            END) = 0 THEN 'Unknown'
                            WHEN MAX(CASE
                                WHEN ba.[Strategic Status] IN ('Strategic', 'Core', 'Core Enterprise Application', 'Yes', 'True') THEN 1
                                ELSE 0
                            END) = 1 THEN 'Strategic'
                            ELSE 'Non-Strategic'
                        END AS strategic_status
                    FROM TR_UB AS t
                    LEFT JOIN [Business Applications] AS ba
                        ON ba._id = t.TR_id
                    GROUP BY t.UB_id
                ) AS apps
                    ON apps.asset_id = e.UBId
            ), 
            asset_incidents AS (
                SELECT
                    x.UBId AS asset_id,
                    i.TXId AS incident_id,
                    i.TX_CRE AS incident_created,
                    i.TX_BUS AS business_duration,
                    CASE
                        WHEN i.TX_PRI LIKE '1%' THEN 1
                        WHEN i.TX_PRI LIKE '2%' THEN 2
                        WHEN i.TX_PRI LIKE '3%' THEN 3
                        WHEN i.TX_PRI LIKE '4%' THEN 4
                        WHEN i.TX_PRI LIKE '5%' THEN 5
                        ELSE NULL
                    END AS priority_num
                FROM landscapeQuery_strategy_A.TX_UB AS x
                JOIN landscapeQuery_strategy_A.TX AS i
                    ON i.TXId = x.TXId
            ), 
            change_windows AS (
                SELECT
                    ca.change_id,
                    ca.asset_id,
                    ca.change_type,
                    ca.change_date,
                    ca.virtualization_technology,
                    ca.strategic_status,
                    w.window_days,
                    (0 - w.window_days) AS neg_window_days
                FROM change_assets AS ca
                CROSS JOIN windows AS w
            ), 
            changed_asset_metrics AS (
                SELECT
                    cw.window_days,
                    cw.change_type,
                    cw.virtualization_technology,
                    cw.strategic_status,
                    cw.change_id,
                    cw.asset_id,
                    COUNT(CASE
                        WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date) AND
                        ai.incident_created < cw.change_date THEN 1
                    END) AS pre_incident_count,
                    COUNT(CASE
                        WHEN ai.incident_created >= cw.change_date AND
                        ai.incident_created < DATEADD(day, cw.window_days, cw.change_date) THEN 1
                    END) AS post_incident_count,
                    AVG(CASE
                        WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date) AND
                        ai.incident_created < cw.change_date THEN CAST(ai.priority_num AS FLOAT)
                    END) AS pre_priority_avg,
                    AVG(CASE
                        WHEN ai.incident_created >= cw.change_date AND
                        ai.incident_created < DATEADD(day, cw.window_days, cw.change_date) THEN CAST(ai.priority_num AS FLOAT)
                    END) AS post_priority_avg,
                    AVG(CASE
                        WHEN ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date) AND
                        ai.incident_created < cw.change_date THEN CAST(ai.business_duration AS FLOAT)
                    END) AS pre_business_duration_avg,
                    AVG(CASE
                        WHEN ai.incident_created >= cw.change_date AND
                        ai.incident_created < DATEADD(day, cw.window_days, cw.change_date) THEN CAST(ai.business_duration AS FLOAT)
                    END) AS post_business_duration_avg
                FROM change_windows AS cw
                LEFT JOIN asset_incidents AS ai
                    ON ai.asset_id = cw.asset_id AND
                    ai.incident_created >= DATEADD(day, cw.neg_window_days, cw.change_date) AND
                    ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                GROUP BY
                    cw.window_days,
                    cw.change_type,
                    cw.virtualization_technology,
                    cw.strategic_status,
                    cw.change_id,
                    cw.asset_id
            ), 
            control_asset_metrics AS (
                SELECT
                    cw.window_days,
                    cw.change_type,
                    cw.virtualization_technology,
                    cw.strategic_status,
                    cw.change_id,
                    COUNT(DISTINCT ai.incident_id) AS control_post_incident_count,
                    AVG(CAST(ai.priority_num AS FLOAT)) AS control_post_priority_avg,
                    AVG(CAST(ai.business_duration AS FLOAT)) AS control_post_business_duration_avg
                FROM change_windows AS cw
                JOIN landscapeQuery_strategy_A.UB AS sa2
                    ON sa2.UBId <> cw.asset_id
                LEFT JOIN landscapeQuery_strategy_A.EA_UB AS e2
                    ON e2.UBId = sa2.UBId
                LEFT JOIN landscapeQuery_strategy_A.EA AS c2
                    ON c2.EAId = e2.EAId AND
                    c2.EA_DAT >= DATEADD(day, cw.neg_window_days, cw.change_date) AND
                    c2.EA_DAT < DATEADD(day, cw.window_days, cw.change_date)
                LEFT JOIN asset_incidents AS ai
                    ON ai.asset_id = sa2.UBId AND
                    ai.incident_created >= cw.change_date AND
                    ai.incident_created < DATEADD(day, cw.window_days, cw.change_date)
                WHERE c2.EAId IS NULL
                GROUP BY
                    cw.window_days,
                    cw.change_type,
                    cw.virtualization_technology,
                    cw.strategic_status,
                    cw.change_id
            )
            SELECT
                cam.window_days,
                cam.change_type,
                cam.virtualization_technology,
                cam.strategic_status,
                COUNT(*) AS changed_asset_change_events,
                AVG(CAST(cam.post_incident_count AS FLOAT)) AS mean_post_incident_count,
                VARP(CAST(cam.post_incident_count AS FLOAT)) AS var_post_incident_count,
                AVG(CAST(cam.pre_incident_count AS FLOAT)) AS mean_pre_incident_count,
                VARP(CAST(cam.pre_incident_count AS FLOAT)) AS var_pre_incident_count,
                AVG(cam.post_priority_avg) AS mean_post_priority,
                VARP(cam.post_priority_avg) AS var_post_priority,
                AVG(cam.pre_priority_avg) AS mean_pre_priority,
                VARP(cam.pre_priority_avg) AS var_pre_priority,
                AVG(cam.post_business_duration_avg) AS mean_post_business_duration,
                VARP(cam.post_business_duration_avg) AS var_post_business_duration,
                AVG(cam.pre_business_duration_avg) AS mean_pre_business_duration,
                VARP(cam.pre_business_duration_avg) AS var_pre_business_duration,
                AVG(CAST(cam.post_incident_count - cam.pre_incident_count AS FLOAT)) AS mean_incident_count_delta,
                AVG(cam.post_priority_avg - cam.pre_priority_avg) AS mean_priority_delta,
                AVG(cam.post_business_duration_avg - cam.pre_business_duration_avg) AS mean_business_duration_delta,
                AVG(CAST(ctrl.control_post_incident_count AS FLOAT)) AS mean_control_post_incident_count,
                VARP(CAST(ctrl.control_post_incident_count AS FLOAT)) AS var_control_post_incident_count,
                AVG(ctrl.control_post_priority_avg) AS mean_control_post_priority,
                VARP(ctrl.control_post_priority_avg) AS var_control_post_priority,
                AVG(ctrl.control_post_business_duration_avg) AS mean_control_post_business_duration,
                VARP(ctrl.control_post_business_duration_avg) AS var_control_post_business_duration
            FROM changed_asset_metrics AS cam
            LEFT JOIN control_asset_metrics AS ctrl
                ON ctrl.change_id = cam.change_id AND
                ctrl.window_days = cam.window_days AND
                ctrl.change_type = cam.change_type AND
                ctrl.virtualization_technology = cam.virtualization_technology AND
                ctrl.strategic_status = cam.strategic_status
            GROUP BY
                cam.window_days,
                cam.change_type,
                cam.virtualization_technology,
                cam.strategic_status
            ORDER BY
                cam.window_days,
                changed_asset_change_events DESC,
                cam.change_type,
                cam.virtualization_technology,
                cam.strategic_status
            """);

    [Fact]
    public void Minimal()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
                SELECT
                   VAR_SAMP(c.[Date applied]) AS change_applied_at
                FROM [Changes] c
            """,
            """
            SELECT VARP(c.EA_DAT) AS change_applied_at
            FROM landscapeQuery_strategy_A.EA AS c
            """);
}
