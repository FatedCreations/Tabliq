
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/12
/// </summary>
public class Issue12
{
    [Fact]
    public void Test()
         => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            WITH asset_app AS (
                SELECT
                    sa._id AS server_asset_id,
                    sa.[Virtualization Technology] AS virtualization_technology,
                    sa.[Supported Function] AS supported_function,
                    CASE
                        WHEN MAX(CASE WHEN ba.[Strategic Status] IS NOT NULL THEN 1 ELSE 0 END) = 0 THEN 'Unknown'
                        WHEN MAX(CASE WHEN UPPER(ba.[Strategic Status]) LIKE '%STRATEGIC%' OR UPPER(ba.[Strategic Status]) LIKE '%CORE%' THEN 1 ELSE 0 END) = 1 THEN 'Strategic'
                        ELSE 'Non-Strategic'
                    END AS strategic_application_status
                FROM [Server Assets] sa
                LEFT JOIN [TR_UB] tr
                    ON tr.UB_id = sa._id
                LEFT JOIN [Business Applications] ba
                    ON ba._id = tr.TR_id
                GROUP BY
                    sa._id,
                    sa.[Virtualization Technology],
                    sa.[Supported Function]
            ),
            incident_asset AS (
                SELECT DISTINCT
                    tx.UB_id AS server_asset_id,
                    i._id AS incident_id,
                    i.[Business Duration] AS business_duration,
                    i.[Priority] AS priority,
                    i.[Automation Status] AS automation_status
                FROM [TX_UB] tx
                INNER JOIN [Incidents] i
                    ON i._id = tx.TX_id
            ),
            ordered AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    ia.business_duration,
                    ROW_NUMBER() OVER (
                        PARTITION BY aa.virtualization_technology, aa.supported_function, aa.strategic_application_status
                        ORDER BY ia.business_duration
                    ) AS rn,
                    COUNT(*) OVER (
                        PARTITION BY aa.virtualization_technology, aa.supported_function, aa.strategic_application_status
                    ) AS cnt
                FROM asset_app aa
                JOIN incident_asset ia
                    ON ia.server_asset_id = aa.server_asset_id
                WHERE ia.business_duration IS NOT NULL
            ),
            median_duration AS (
                SELECT
                    virtualization_technology,
                    supported_function,
                    strategic_application_status,
                    AVG(CAST(business_duration AS DECIMAL(18,2))) AS median_business_duration
                FROM ordered
                WHERE rn IN ((cnt + 1) / 2, (cnt + 2) / 2)
                GROUP BY
                    virtualization_technology,
                    supported_function,
                    strategic_application_status
            ),
            asset_counts AS (
                SELECT
                    virtualization_technology,
                    supported_function,
                    strategic_application_status,
                    COUNT(DISTINCT server_asset_id) AS server_asset_count
                FROM asset_app
                GROUP BY
                    virtualization_technology,
                    supported_function,
                    strategic_application_status
            ),
            incident_summary AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    COUNT(DISTINCT ia.incident_id) AS incident_count,
                    AVG(CAST(ia.business_duration AS DECIMAL(18,2))) AS mean_business_duration
                FROM asset_app aa
                LEFT JOIN incident_asset ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            ),
            priority_mix AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    SUM(CASE WHEN ia.priority = '1 - Critical' THEN 1 ELSE 0 END) AS priority_1_critical,
                    SUM(CASE WHEN ia.priority = '2 - High' THEN 1 ELSE 0 END) AS priority_2_high,
                    SUM(CASE WHEN ia.priority = '3 - Moderate' THEN 1 ELSE 0 END) AS priority_3_moderate,
                    SUM(CASE WHEN ia.priority = '4 - Low' THEN 1 ELSE 0 END) AS priority_4_low,
                    SUM(CASE WHEN ia.priority IS NULL THEN 1 ELSE 0 END) AS priority_unknown
                FROM asset_app aa
                LEFT JOIN incident_asset ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            ),
            automation_mix AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    SUM(CASE WHEN ia.automation_status = '1' THEN 1 ELSE 0 END) AS automation_status_1,
                    SUM(CASE WHEN ia.automation_status = '2' THEN 1 ELSE 0 END) AS automation_status_2,
                    SUM(CASE WHEN ia.automation_status = '3' THEN 1 ELSE 0 END) AS automation_status_3,
                    SUM(CASE WHEN ia.automation_status = '4' THEN 1 ELSE 0 END) AS automation_status_4,
                    SUM(CASE WHEN ia.automation_status = '5' THEN 1 ELSE 0 END) AS automation_status_5,
                    SUM(CASE WHEN ia.automation_status IS NULL THEN 1 ELSE 0 END) AS automation_status_unknown
                FROM asset_app aa
                LEFT JOIN incident_asset ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            )
            SELECT
                ac.virtualization_technology,
                ac.supported_function,
                ac.strategic_application_status,
                ac.server_asset_count,
                COALESCE(isu.incident_count, 0) AS incident_count,
                CASE
                    WHEN ac.server_asset_count = 0 THEN NULL
                    ELSE CAST(COALESCE(isu.incident_count, 0) AS DECIMAL(18,2)) / ac.server_asset_count
                END AS incidents_per_asset,
                isu.mean_business_duration,
                md.median_business_duration,
                pm.priority_1_critical,
                pm.priority_2_high,
                pm.priority_3_moderate,
                pm.priority_4_low,
                pm.priority_unknown,
                am.automation_status_1,
                am.automation_status_2,
                am.automation_status_3,
                am.automation_status_4,
                am.automation_status_5,
                am.automation_status_unknown
            FROM asset_counts ac
            LEFT JOIN incident_summary isu
                ON isu.virtualization_technology = ac.virtualization_technology
               AND isu.supported_function = ac.supported_function
               AND isu.strategic_application_status = ac.strategic_application_status
            LEFT JOIN median_duration md
                ON md.virtualization_technology = ac.virtualization_technology
               AND md.supported_function = ac.supported_function
               AND md.strategic_application_status = ac.strategic_application_status
            LEFT JOIN priority_mix pm
                ON pm.virtualization_technology = ac.virtualization_technology
               AND pm.supported_function = ac.supported_function
               AND pm.strategic_application_status = ac.strategic_application_status
            LEFT JOIN automation_mix am
                ON am.virtualization_technology = ac.virtualization_technology
               AND am.supported_function = ac.supported_function
               AND am.strategic_application_status = ac.strategic_application_status
            ORDER BY ac.server_asset_count DESC, ac.virtualization_technology, ac.supported_function, ac.strategic_application_status;
            """,
            """
            WITH asset_app AS (
                SELECT
                    sa.UBId AS server_asset_id,
                    sa.UB_IRT AS virtualization_technology,
                    sa.UB_FUN AS supported_function,
                    CASE
                        WHEN MAX(CASE
                            WHEN ba.TR_STR IS NOT NULL THEN 1
                            ELSE 0
                        END) = 0 THEN 'Unknown'
                        WHEN MAX(CASE
                            WHEN UPPER(ba.TR_STR) LIKE '%STRATEGIC%' OR
                            UPPER(ba.TR_STR) LIKE '%CORE%' THEN 1
                            ELSE 0
                        END) = 1 THEN 'Strategic'
                        ELSE 'Non-Strategic'
                    END AS strategic_application_status
                FROM landscapeQuery_strategy_A.UB AS sa
                LEFT JOIN landscapeQuery_strategy_A.TR_UB AS tr
                    ON tr.UBId = sa.UBId
                LEFT JOIN landscapeQuery_strategy_A.TR AS ba
                    ON ba.TRId = tr.TRId
                GROUP BY
                    sa.UBId,
                    sa.UB_IRT,
                    sa.UB_FUN
            ), 
            incident_asset AS (
                SELECT DISTINCT
                    tx.UBId AS server_asset_id,
                    i.TXId AS incident_id,
                    i.TX_BUS AS business_duration,
                    i.TX_PRI AS priority,
                    i.TX_AUT AS automation_status
                FROM landscapeQuery_strategy_A.TX_UB AS tx
                INNER JOIN landscapeQuery_strategy_A.TX AS i
                    ON i.TXId = tx.TXId
            ), 
            ordered AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    ia.business_duration,
                    ROW_NUMBER() OVER (PARTITION BY aa.virtualization_technology, aa.supported_function, aa.strategic_application_status ORDER BY ia.business_duration) AS rn,
                    COUNT(*) OVER (PARTITION BY aa.virtualization_technology, aa.supported_function, aa.strategic_application_status) AS cnt
                FROM asset_app AS aa
                JOIN incident_asset AS ia
                    ON ia.server_asset_id = aa.server_asset_id
                WHERE ia.business_duration IS NOT NULL
            ), 
            median_duration AS (
                SELECT
                    virtualization_technology,
                    supported_function,
                    strategic_application_status,
                    AVG(CAST(business_duration AS DECIMAL(18, 2))) AS median_business_duration
                FROM ordered
                WHERE rn IN ((cnt + 1) / 2, (cnt + 2) / 2)
                GROUP BY
                    virtualization_technology,
                    supported_function,
                    strategic_application_status
            ), 
            asset_counts AS (
                SELECT
                    virtualization_technology,
                    supported_function,
                    strategic_application_status,
                    COUNT(DISTINCT server_asset_id) AS server_asset_count
                FROM asset_app
                GROUP BY
                    virtualization_technology,
                    supported_function,
                    strategic_application_status
            ), 
            incident_summary AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    COUNT(DISTINCT ia.incident_id) AS incident_count,
                    AVG(CAST(ia.business_duration AS DECIMAL(18, 2))) AS mean_business_duration
                FROM asset_app AS aa
                LEFT JOIN incident_asset AS ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            ), 
            priority_mix AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    SUM(CASE
                        WHEN ia.priority = '1 - Critical' THEN 1
                        ELSE 0
                    END) AS priority_1_critical,
                    SUM(CASE
                        WHEN ia.priority = '2 - High' THEN 1
                        ELSE 0
                    END) AS priority_2_high,
                    SUM(CASE
                        WHEN ia.priority = '3 - Moderate' THEN 1
                        ELSE 0
                    END) AS priority_3_moderate,
                    SUM(CASE
                        WHEN ia.priority = '4 - Low' THEN 1
                        ELSE 0
                    END) AS priority_4_low,
                    SUM(CASE
                        WHEN ia.priority IS NULL THEN 1
                        ELSE 0
                    END) AS priority_unknown
                FROM asset_app AS aa
                LEFT JOIN incident_asset AS ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            ), 
            automation_mix AS (
                SELECT
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status,
                    SUM(CASE
                        WHEN ia.automation_status = '1' THEN 1
                        ELSE 0
                    END) AS automation_status_1,
                    SUM(CASE
                        WHEN ia.automation_status = '2' THEN 1
                        ELSE 0
                    END) AS automation_status_2,
                    SUM(CASE
                        WHEN ia.automation_status = '3' THEN 1
                        ELSE 0
                    END) AS automation_status_3,
                    SUM(CASE
                        WHEN ia.automation_status = '4' THEN 1
                        ELSE 0
                    END) AS automation_status_4,
                    SUM(CASE
                        WHEN ia.automation_status = '5' THEN 1
                        ELSE 0
                    END) AS automation_status_5,
                    SUM(CASE
                        WHEN ia.automation_status IS NULL THEN 1
                        ELSE 0
                    END) AS automation_status_unknown
                FROM asset_app AS aa
                LEFT JOIN incident_asset AS ia
                    ON ia.server_asset_id = aa.server_asset_id
                GROUP BY
                    aa.virtualization_technology,
                    aa.supported_function,
                    aa.strategic_application_status
            )
            SELECT
                ac.virtualization_technology,
                ac.supported_function,
                ac.strategic_application_status,
                ac.server_asset_count,
                COALESCE(isu.incident_count, 0) AS incident_count,
                CASE
                    WHEN ac.server_asset_count = 0 THEN NULL
                    ELSE CAST(COALESCE(isu.incident_count, 0) AS DECIMAL(18, 2)) / ac.server_asset_count
                END AS incidents_per_asset,
                isu.mean_business_duration,
                md.median_business_duration,
                pm.priority_1_critical,
                pm.priority_2_high,
                pm.priority_3_moderate,
                pm.priority_4_low,
                pm.priority_unknown,
                am.automation_status_1,
                am.automation_status_2,
                am.automation_status_3,
                am.automation_status_4,
                am.automation_status_5,
                am.automation_status_unknown
            FROM asset_counts AS ac
            LEFT JOIN incident_summary AS isu
                ON isu.virtualization_technology = ac.virtualization_technology AND
                isu.supported_function = ac.supported_function AND
                isu.strategic_application_status = ac.strategic_application_status
            LEFT JOIN median_duration AS md
                ON md.virtualization_technology = ac.virtualization_technology AND
                md.supported_function = ac.supported_function AND
                md.strategic_application_status = ac.strategic_application_status
            LEFT JOIN priority_mix AS pm
                ON pm.virtualization_technology = ac.virtualization_technology AND
                pm.supported_function = ac.supported_function AND
                pm.strategic_application_status = ac.strategic_application_status
            LEFT JOIN automation_mix AS am
                ON am.virtualization_technology = ac.virtualization_technology AND
                am.supported_function = ac.supported_function AND
                am.strategic_application_status = ac.strategic_application_status
            ORDER BY
                ac.server_asset_count DESC,
                ac.virtualization_technology,
                ac.supported_function,
                ac.strategic_application_status
            """);
}
