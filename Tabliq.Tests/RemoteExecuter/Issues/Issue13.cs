
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/13
/// </summary>
public class Issue13
{
    [Fact]
    public void Test()
         => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH asset_app AS (
                SELECT
                    sa._id AS asset_id,
                    sa.[Supported Function] AS supported_function,
                    ba.[Strategic Status] AS strategic_status,
                    CASE
                        WHEN sa.[Virtualization Technology] = 'Cloud' THEN 'Cloud'
                        WHEN sa.[Virtualization Technology] IS NOT NULL AND sa.[Virtualization Technology] <> 'Cloud' THEN 'Legacy'
                        ELSE NULL
                    END AS host_class
                FROM [Server Assets] sa
                INNER JOIN TR_UB tr
                    ON tr.UB_id = sa._id
                INNER JOIN [Business Applications] ba
                    ON ba._id = tr.TR_id
                WHERE sa.[Supported Function] IS NOT NULL
                  AND ba.[Strategic Status] IS NOT NULL
                  AND sa.[Virtualization Technology] IS NOT NULL
            ), asset_cells AS (
                SELECT DISTINCT
                    asset_id,
                    supported_function,
                    strategic_status,
                    host_class
                FROM asset_app
                WHERE host_class IN ('Cloud','Legacy')
            ), matched_cells AS (
                SELECT
                    supported_function,
                    strategic_status
                FROM asset_cells
                GROUP BY supported_function, strategic_status
                HAVING COUNT(DISTINCT host_class) = 2
            ), matched_assets AS (
                SELECT ac.asset_id, ac.supported_function, ac.strategic_status, ac.host_class
                FROM asset_cells ac
                INNER JOIN matched_cells mc
                    ON mc.supported_function = ac.supported_function
                   AND mc.strategic_status = ac.strategic_status
            ), asset_base AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    COUNT(DISTINCT asset_id) AS asset_count
                FROM matched_assets
                GROUP BY supported_function, strategic_status, host_class
            ), incident_base AS (
                SELECT DISTINCT
                    ma.supported_function,
                    ma.strategic_status,
                    ma.host_class,
                    ma.asset_id,
                    i._id AS incident_id,
                    i.[Business Duration] AS business_duration,
                    i.[Priority] AS priority,
                    i.[Automation Status] AS automation_status
                FROM matched_assets ma
                LEFT JOIN TX_UB tx
                    ON tx.UB_id = ma.asset_id
                LEFT JOIN [Incidents] i
                    ON i._id = tx.TX_id
            ), incident_counts AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    COUNT(DISTINCT incident_id) AS incident_count
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY supported_function, strategic_status, host_class
            ), mean_duration AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CAST(business_duration AS FLOAT)) AS mean_business_duration
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY supported_function, strategic_status, host_class
            ), median_prep AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    business_duration,
                    ROW_NUMBER() OVER (
                        PARTITION BY supported_function, strategic_status, host_class
                        ORDER BY business_duration
                    ) AS rn,
                    COUNT(*) OVER (
                        PARTITION BY supported_function, strategic_status, host_class
                    ) AS cnt
                FROM incident_base
                WHERE incident_id IS NOT NULL
            ), median_duration AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CAST(business_duration AS FLOAT)) AS median_business_duration
                FROM median_prep
                WHERE rn IN ((cnt + 1) / 2, (cnt + 2) / 2)
                GROUP BY supported_function, strategic_status, host_class
            ), priority_shares AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CASE WHEN priority = '1 - Critical' THEN 1.0 ELSE 0.0 END) AS p1_share,
                    AVG(CASE WHEN priority = '2 - High' THEN 1.0 ELSE 0.0 END) AS p2_share,
                    AVG(CASE WHEN priority = '3 - Medium' THEN 1.0 ELSE 0.0 END) AS p3_share,
                    AVG(CASE WHEN priority = '4 - Low' THEN 1.0 ELSE 0.0 END) AS p4_share
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY supported_function, strategic_status, host_class
            ), automation_shares AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CASE WHEN automation_status = '1. No Automation' THEN 1.0 ELSE 0.0 END) AS auto_1_share,
                    AVG(CASE WHEN automation_status = '2. Raised by Integration, Handled Manually' THEN 1.0 ELSE 0.0 END) AS auto_2_share,
                    AVG(CASE WHEN automation_status = '3. Raised Automatically, Handled Manually' THEN 1.0 ELSE 0.0 END) AS auto_3_share,
                    AVG(CASE WHEN automation_status = '4. Proactive Remediation' THEN 1.0 ELSE 0.0 END) AS auto_4_share,
                    AVG(CASE WHEN automation_status = '5. Attempted Automated Remediation' THEN 1.0 ELSE 0.0 END) AS auto_5_share,
                    AVG(CASE WHEN automation_status = '6. Fully Automated Remediation' THEN 1.0 ELSE 0.0 END) AS auto_6_share
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY supported_function, strategic_status, host_class
            ), cell_metrics AS (
                SELECT
                    ab.supported_function,
                    ab.strategic_status,
                    ab.host_class,
                    ab.asset_count,
                    COALESCE(ic.incident_count, 0) AS incident_count,
                    CASE WHEN ab.asset_count > 0 THEN COALESCE(ic.incident_count, 0) * 1.0 / ab.asset_count ELSE NULL END AS incidents_per_asset,
                    md.mean_business_duration,
                    med.median_business_duration,
                    ps.p1_share,
                    ps.p2_share,
                    ps.p3_share,
                    ps.p4_share,
                    aus.auto_1_share,
                    aus.auto_2_share,
                    aus.auto_3_share,
                    aus.auto_4_share,
                    aus.auto_5_share,
                    aus.auto_6_share
                FROM asset_base ab
                LEFT JOIN incident_counts ic
                    ON ic.supported_function = ab.supported_function
                   AND ic.strategic_status = ab.strategic_status
                   AND ic.host_class = ab.host_class
                LEFT JOIN mean_duration md
                    ON md.supported_function = ab.supported_function
                   AND md.strategic_status = ab.strategic_status
                   AND md.host_class = ab.host_class
                LEFT JOIN median_duration med
                    ON med.supported_function = ab.supported_function
                   AND med.strategic_status = ab.strategic_status
                   AND med.host_class = ab.host_class
                LEFT JOIN priority_shares ps
                    ON ps.supported_function = ab.supported_function
                   AND ps.strategic_status = ab.strategic_status
                   AND ps.host_class = ab.host_class
                LEFT JOIN automation_shares aus
                    ON aus.supported_function = ab.supported_function
                   AND aus.strategic_status = ab.strategic_status
                   AND aus.host_class = ab.host_class
            ), matched_deltas AS (
                SELECT
                    l.supported_function,
                    l.strategic_status,
                    l.asset_count AS legacy_asset_count,
                    c.asset_count AS cloud_asset_count,
                    l.incident_count AS legacy_incident_count,
                    c.incident_count AS cloud_incident_count,
                    l.incidents_per_asset AS legacy_incidents_per_asset,
                    c.incidents_per_asset AS cloud_incidents_per_asset,
                    l.incidents_per_asset - c.incidents_per_asset AS delta_incidents_per_asset,
                    l.mean_business_duration AS legacy_mean_business_duration,
                    c.mean_business_duration AS cloud_mean_business_duration,
                    l.mean_business_duration - c.mean_business_duration AS delta_mean_business_duration,
                    l.median_business_duration AS legacy_median_business_duration,
                    c.median_business_duration AS cloud_median_business_duration,
                    l.median_business_duration - c.median_business_duration AS delta_median_business_duration,
                    l.p1_share - c.p1_share AS delta_p1_share,
                    l.p2_share - c.p2_share AS delta_p2_share,
                    l.p3_share - c.p3_share AS delta_p3_share,
                    l.p4_share - c.p4_share AS delta_p4_share,
                    l.auto_1_share - c.auto_1_share AS delta_auto_1_share,
                    l.auto_2_share - c.auto_2_share AS delta_auto_2_share,
                    l.auto_3_share - c.auto_3_share AS delta_auto_3_share,
                    l.auto_4_share - c.auto_4_share AS delta_auto_4_share,
                    l.auto_5_share - c.auto_5_share AS delta_auto_5_share,
                    l.auto_6_share - c.auto_6_share AS delta_auto_6_share
                FROM cell_metrics l
                INNER JOIN cell_metrics c
                    ON c.supported_function = l.supported_function
                   AND c.strategic_status = l.strategic_status
                   AND c.host_class = 'Cloud'
                WHERE l.host_class = 'Legacy'
            ), aggregate_summary AS (
                SELECT
                    'AGGREGATE_MATCHED_DELTAS' AS row_type,
                    CAST(NULL AS NVARCHAR(4000)) AS supported_function,
                    CAST(NULL AS NVARCHAR(4000)) AS strategic_status,
                    COUNT(*) AS matched_cells,
                    SUM(legacy_asset_count) AS legacy_asset_count,
                    SUM(cloud_asset_count) AS cloud_asset_count,
                    SUM(legacy_incident_count) AS legacy_incident_count,
                    SUM(cloud_incident_count) AS cloud_incident_count,
                    AVG(delta_incidents_per_asset) AS avg_cell_delta_incidents_per_asset,
                    SUM(legacy_incident_count) * 1.0 / NULLIF(SUM(legacy_asset_count),0)
                      - SUM(cloud_incident_count) * 1.0 / NULLIF(SUM(cloud_asset_count),0) AS pooled_delta_incidents_per_asset,
                    AVG(delta_mean_business_duration) AS avg_cell_delta_mean_business_duration,
                    AVG(delta_median_business_duration) AS avg_cell_delta_median_business_duration,
                    AVG(delta_p1_share) AS avg_cell_delta_p1_share,
                    AVG(delta_p2_share) AS avg_cell_delta_p2_share,
                    AVG(delta_p3_share) AS avg_cell_delta_p3_share,
                    AVG(delta_p4_share) AS avg_cell_delta_p4_share,
                    AVG(delta_auto_1_share) AS avg_cell_delta_auto_1_share,
                    AVG(delta_auto_2_share) AS avg_cell_delta_auto_2_share,
                    AVG(delta_auto_3_share) AS avg_cell_delta_auto_3_share,
                    AVG(delta_auto_4_share) AS avg_cell_delta_auto_4_share,
                    AVG(delta_auto_5_share) AS avg_cell_delta_auto_5_share,
                    AVG(delta_auto_6_share) AS avg_cell_delta_auto_6_share
                FROM matched_deltas
            ), cell_output AS (
                SELECT
                    'CELL_DELTA' AS row_type,
                    supported_function,
                    strategic_status,
                    CAST(NULL AS BIGINT) AS matched_cells,
                    legacy_asset_count,
                    cloud_asset_count,
                    legacy_incident_count,
                    cloud_incident_count,
                    delta_incidents_per_asset AS avg_cell_delta_incidents_per_asset,
                    CAST(NULL AS FLOAT) AS pooled_delta_incidents_per_asset,
                    delta_mean_business_duration AS avg_cell_delta_mean_business_duration,
                    delta_median_business_duration AS avg_cell_delta_median_business_duration,
                    delta_p1_share AS avg_cell_delta_p1_share,
                    delta_p2_share AS avg_cell_delta_p2_share,
                    delta_p3_share AS avg_cell_delta_p3_share,
                    delta_p4_share AS avg_cell_delta_p4_share,
                    delta_auto_1_share AS avg_cell_delta_auto_1_share,
                    delta_auto_2_share AS avg_cell_delta_auto_2_share,
                    delta_auto_3_share AS avg_cell_delta_auto_3_share,
                    delta_auto_4_share AS avg_cell_delta_auto_4_share,
                    delta_auto_5_share AS avg_cell_delta_auto_5_share,
                    delta_auto_6_share AS avg_cell_delta_auto_6_share
                FROM matched_deltas
            )
            SELECT *
            FROM aggregate_summary
            UNION ALL
            SELECT *
            FROM cell_output
            ORDER BY row_type, avg_cell_delta_incidents_per_asset DESC, supported_function, strategic_status
            """,
            """
                        WITH asset_app AS (
                SELECT
                    sa.UBId AS asset_id,
                    sa.UB_FUN AS supported_function,
                    ba.TR_STR AS strategic_status,
                    CASE
                        WHEN sa.UB_IRT = 'Cloud' THEN 'Cloud'
                        WHEN sa.UB_IRT IS NOT NULL AND
                        sa.UB_IRT <> 'Cloud' THEN 'Legacy'
                        ELSE NULL
                    END AS host_class
                FROM landscapeQuery_strategy_A.UB AS sa
                INNER JOIN landscapeQuery_strategy_A.TR_UB AS tr
                    ON tr.UBId = sa.UBId
                INNER JOIN landscapeQuery_strategy_A.TR AS ba
                    ON ba.TRId = tr.TRId
                WHERE
                    sa.UB_FUN IS NOT NULL AND
                    ba.TR_STR IS NOT NULL AND
                    sa.UB_IRT IS NOT NULL
            ), 
            asset_cells AS (
                SELECT DISTINCT
                    asset_id,
                    supported_function,
                    strategic_status,
                    host_class
                FROM asset_app
                WHERE host_class IN ('Cloud', 'Legacy')
            ), 
            matched_cells AS (
                SELECT
                    supported_function,
                    strategic_status
                FROM asset_cells
                GROUP BY
                    supported_function,
                    strategic_status
                HAVING COUNT(DISTINCT host_class) = 2
            ), 
            matched_assets AS (
                SELECT
                    ac.asset_id,
                    ac.supported_function,
                    ac.strategic_status,
                    ac.host_class
                FROM asset_cells AS ac
                INNER JOIN matched_cells AS mc
                    ON mc.supported_function = ac.supported_function AND
                    mc.strategic_status = ac.strategic_status
            ), 
            asset_base AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    COUNT(DISTINCT asset_id) AS asset_count
                FROM matched_assets
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            incident_base AS (
                SELECT DISTINCT
                    ma.supported_function,
                    ma.strategic_status,
                    ma.host_class,
                    ma.asset_id,
                    i.TXId AS incident_id,
                    i.TX_BUS AS business_duration,
                    i.TX_PRI AS priority,
                    i.TX_AUT AS automation_status
                FROM matched_assets AS ma
                LEFT JOIN landscapeQuery_strategy_A.TX_UB AS tx
                    ON tx.UBId = ma.asset_id
                LEFT JOIN landscapeQuery_strategy_A.TX AS i
                    ON i.TXId = tx.TXId
            ), 
            incident_counts AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    COUNT(DISTINCT incident_id) AS incident_count
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            mean_duration AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CAST(business_duration AS FLOAT)) AS mean_business_duration
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            median_prep AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    business_duration,
                    ROW_NUMBER() OVER (PARTITION BY supported_function, strategic_status, host_class ORDER BY business_duration) AS rn,
                    COUNT(*) OVER (PARTITION BY supported_function, strategic_status, host_class) AS cnt
                FROM incident_base
                WHERE incident_id IS NOT NULL
            ), 
            median_duration AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CAST(business_duration AS FLOAT)) AS median_business_duration
                FROM median_prep
                WHERE rn IN ((cnt + 1) / 2, (cnt + 2) / 2)
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            priority_shares AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CASE
                        WHEN priority = '1 - Critical' THEN 1.0
                        ELSE 0.0
                    END) AS p1_share,
                    AVG(CASE
                        WHEN priority = '2 - High' THEN 1.0
                        ELSE 0.0
                    END) AS p2_share,
                    AVG(CASE
                        WHEN priority = '3 - Medium' THEN 1.0
                        ELSE 0.0
                    END) AS p3_share,
                    AVG(CASE
                        WHEN priority = '4 - Low' THEN 1.0
                        ELSE 0.0
                    END) AS p4_share
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            automation_shares AS (
                SELECT
                    supported_function,
                    strategic_status,
                    host_class,
                    AVG(CASE
                        WHEN automation_status = '1. No Automation' THEN 1.0
                        ELSE 0.0
                    END) AS auto_1_share,
                    AVG(CASE
                        WHEN automation_status = '2. Raised by Integration, Handled Manually' THEN 1.0
                        ELSE 0.0
                    END) AS auto_2_share,
                    AVG(CASE
                        WHEN automation_status = '3. Raised Automatically, Handled Manually' THEN 1.0
                        ELSE 0.0
                    END) AS auto_3_share,
                    AVG(CASE
                        WHEN automation_status = '4. Proactive Remediation' THEN 1.0
                        ELSE 0.0
                    END) AS auto_4_share,
                    AVG(CASE
                        WHEN automation_status = '5. Attempted Automated Remediation' THEN 1.0
                        ELSE 0.0
                    END) AS auto_5_share,
                    AVG(CASE
                        WHEN automation_status = '6. Fully Automated Remediation' THEN 1.0
                        ELSE 0.0
                    END) AS auto_6_share
                FROM incident_base
                WHERE incident_id IS NOT NULL
                GROUP BY
                    supported_function,
                    strategic_status,
                    host_class
            ), 
            cell_metrics AS (
                SELECT
                    ab.supported_function,
                    ab.strategic_status,
                    ab.host_class,
                    ab.asset_count,
                    COALESCE(ic.incident_count, 0) AS incident_count,
                    CASE
                        WHEN ab.asset_count > 0 THEN COALESCE(ic.incident_count, 0) * 1.0 / ab.asset_count
                        ELSE NULL
                    END AS incidents_per_asset,
                    md.mean_business_duration,
                    med.median_business_duration,
                    ps.p1_share,
                    ps.p2_share,
                    ps.p3_share,
                    ps.p4_share,
                    aus.auto_1_share,
                    aus.auto_2_share,
                    aus.auto_3_share,
                    aus.auto_4_share,
                    aus.auto_5_share,
                    aus.auto_6_share
                FROM asset_base AS ab
                LEFT JOIN incident_counts AS ic
                    ON ic.supported_function = ab.supported_function AND
                    ic.strategic_status = ab.strategic_status AND
                    ic.host_class = ab.host_class
                LEFT JOIN mean_duration AS md
                    ON md.supported_function = ab.supported_function AND
                    md.strategic_status = ab.strategic_status AND
                    md.host_class = ab.host_class
                LEFT JOIN median_duration AS med
                    ON med.supported_function = ab.supported_function AND
                    med.strategic_status = ab.strategic_status AND
                    med.host_class = ab.host_class
                LEFT JOIN priority_shares AS ps
                    ON ps.supported_function = ab.supported_function AND
                    ps.strategic_status = ab.strategic_status AND
                    ps.host_class = ab.host_class
                LEFT JOIN automation_shares AS aus
                    ON aus.supported_function = ab.supported_function AND
                    aus.strategic_status = ab.strategic_status AND
                    aus.host_class = ab.host_class
            ), 
            matched_deltas AS (
                SELECT
                    l.supported_function,
                    l.strategic_status,
                    l.asset_count AS legacy_asset_count,
                    c.asset_count AS cloud_asset_count,
                    l.incident_count AS legacy_incident_count,
                    c.incident_count AS cloud_incident_count,
                    l.incidents_per_asset AS legacy_incidents_per_asset,
                    c.incidents_per_asset AS cloud_incidents_per_asset,
                    l.incidents_per_asset - c.incidents_per_asset AS delta_incidents_per_asset,
                    l.mean_business_duration AS legacy_mean_business_duration,
                    c.mean_business_duration AS cloud_mean_business_duration,
                    l.mean_business_duration - c.mean_business_duration AS delta_mean_business_duration,
                    l.median_business_duration AS legacy_median_business_duration,
                    c.median_business_duration AS cloud_median_business_duration,
                    l.median_business_duration - c.median_business_duration AS delta_median_business_duration,
                    l.p1_share - c.p1_share AS delta_p1_share,
                    l.p2_share - c.p2_share AS delta_p2_share,
                    l.p3_share - c.p3_share AS delta_p3_share,
                    l.p4_share - c.p4_share AS delta_p4_share,
                    l.auto_1_share - c.auto_1_share AS delta_auto_1_share,
                    l.auto_2_share - c.auto_2_share AS delta_auto_2_share,
                    l.auto_3_share - c.auto_3_share AS delta_auto_3_share,
                    l.auto_4_share - c.auto_4_share AS delta_auto_4_share,
                    l.auto_5_share - c.auto_5_share AS delta_auto_5_share,
                    l.auto_6_share - c.auto_6_share AS delta_auto_6_share
                FROM cell_metrics AS l
                INNER JOIN cell_metrics AS c
                    ON c.supported_function = l.supported_function AND
                    c.strategic_status = l.strategic_status AND
                    c.host_class = 'Cloud'
                WHERE l.host_class = 'Legacy'
            ), 
            aggregate_summary AS (
                SELECT
                    'AGGREGATE_MATCHED_DELTAS' AS row_type,
                    CAST(NULL AS NVARCHAR(4000)) AS supported_function,
                    CAST(NULL AS NVARCHAR(4000)) AS strategic_status,
                    COUNT(*) AS matched_cells,
                    SUM(legacy_asset_count) AS legacy_asset_count,
                    SUM(cloud_asset_count) AS cloud_asset_count,
                    SUM(legacy_incident_count) AS legacy_incident_count,
                    SUM(cloud_incident_count) AS cloud_incident_count,
                    AVG(delta_incidents_per_asset) AS avg_cell_delta_incidents_per_asset,
                    SUM(legacy_incident_count) * 1.0 / NULLIF(SUM(legacy_asset_count), 0) - SUM(cloud_incident_count) * 1.0 / NULLIF(SUM(cloud_asset_count), 0) AS pooled_delta_incidents_per_asset,
                    AVG(delta_mean_business_duration) AS avg_cell_delta_mean_business_duration,
                    AVG(delta_median_business_duration) AS avg_cell_delta_median_business_duration,
                    AVG(delta_p1_share) AS avg_cell_delta_p1_share,
                    AVG(delta_p2_share) AS avg_cell_delta_p2_share,
                    AVG(delta_p3_share) AS avg_cell_delta_p3_share,
                    AVG(delta_p4_share) AS avg_cell_delta_p4_share,
                    AVG(delta_auto_1_share) AS avg_cell_delta_auto_1_share,
                    AVG(delta_auto_2_share) AS avg_cell_delta_auto_2_share,
                    AVG(delta_auto_3_share) AS avg_cell_delta_auto_3_share,
                    AVG(delta_auto_4_share) AS avg_cell_delta_auto_4_share,
                    AVG(delta_auto_5_share) AS avg_cell_delta_auto_5_share,
                    AVG(delta_auto_6_share) AS avg_cell_delta_auto_6_share
                FROM matched_deltas
            ), 
            cell_output AS (
                SELECT
                    'CELL_DELTA' AS row_type,
                    supported_function,
                    strategic_status,
                    CAST(NULL AS BIGINT) AS matched_cells,
                    legacy_asset_count,
                    cloud_asset_count,
                    legacy_incident_count,
                    cloud_incident_count,
                    delta_incidents_per_asset AS avg_cell_delta_incidents_per_asset,
                    CAST(NULL AS FLOAT) AS pooled_delta_incidents_per_asset,
                    delta_mean_business_duration AS avg_cell_delta_mean_business_duration,
                    delta_median_business_duration AS avg_cell_delta_median_business_duration,
                    delta_p1_share AS avg_cell_delta_p1_share,
                    delta_p2_share AS avg_cell_delta_p2_share,
                    delta_p3_share AS avg_cell_delta_p3_share,
                    delta_p4_share AS avg_cell_delta_p4_share,
                    delta_auto_1_share AS avg_cell_delta_auto_1_share,
                    delta_auto_2_share AS avg_cell_delta_auto_2_share,
                    delta_auto_3_share AS avg_cell_delta_auto_3_share,
                    delta_auto_4_share AS avg_cell_delta_auto_4_share,
                    delta_auto_5_share AS avg_cell_delta_auto_5_share,
                    delta_auto_6_share AS avg_cell_delta_auto_6_share
                FROM matched_deltas
            )
            SELECT
                aggregate_summary.row_type,
                aggregate_summary.supported_function,
                aggregate_summary.strategic_status,
                aggregate_summary.matched_cells,
                aggregate_summary.legacy_asset_count,
                aggregate_summary.cloud_asset_count,
                aggregate_summary.legacy_incident_count,
                aggregate_summary.cloud_incident_count,
                aggregate_summary.avg_cell_delta_incidents_per_asset,
                aggregate_summary.pooled_delta_incidents_per_asset,
                aggregate_summary.avg_cell_delta_mean_business_duration,
                aggregate_summary.avg_cell_delta_median_business_duration,
                aggregate_summary.avg_cell_delta_p1_share,
                aggregate_summary.avg_cell_delta_p2_share,
                aggregate_summary.avg_cell_delta_p3_share,
                aggregate_summary.avg_cell_delta_p4_share,
                aggregate_summary.avg_cell_delta_auto_1_share,
                aggregate_summary.avg_cell_delta_auto_2_share,
                aggregate_summary.avg_cell_delta_auto_3_share,
                aggregate_summary.avg_cell_delta_auto_4_share,
                aggregate_summary.avg_cell_delta_auto_5_share,
                aggregate_summary.avg_cell_delta_auto_6_share
            FROM aggregate_summary
            UNION ALL
            SELECT
                cell_output.row_type,
                cell_output.supported_function,
                cell_output.strategic_status,
                cell_output.matched_cells,
                cell_output.legacy_asset_count,
                cell_output.cloud_asset_count,
                cell_output.legacy_incident_count,
                cell_output.cloud_incident_count,
                cell_output.avg_cell_delta_incidents_per_asset,
                cell_output.pooled_delta_incidents_per_asset,
                cell_output.avg_cell_delta_mean_business_duration,
                cell_output.avg_cell_delta_median_business_duration,
                cell_output.avg_cell_delta_p1_share,
                cell_output.avg_cell_delta_p2_share,
                cell_output.avg_cell_delta_p3_share,
                cell_output.avg_cell_delta_p4_share,
                cell_output.avg_cell_delta_auto_1_share,
                cell_output.avg_cell_delta_auto_2_share,
                cell_output.avg_cell_delta_auto_3_share,
                cell_output.avg_cell_delta_auto_4_share,
                cell_output.avg_cell_delta_auto_5_share,
                cell_output.avg_cell_delta_auto_6_share
            FROM cell_output
            ORDER BY
                row_type,
                avg_cell_delta_incidents_per_asset DESC,
                supported_function,
                strategic_status
            """);
}
