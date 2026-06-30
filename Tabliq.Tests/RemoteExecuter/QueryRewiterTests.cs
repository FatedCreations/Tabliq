using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Parsing;
using Tabliq.Sql.Printer;
using Tabliq.Sql.Rewriter;

namespace Tabliq.Tests.RemoteExecuter;

public class QueryRewiterTests
{
    [Fact]
    public void Issue1()
        => AssertExecuterSql
        .Equal(
            "SELECT [Incident Status] as status," +
            "COUNT(*) as count FROM [TAC SRs]" +
            "GROUP BY [Incident Status]" +
            "ORDER BY count DESC",
            """
            SELECT
                [TAC SRs].SE_NCI AS status,
                COUNT(*) AS count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY [TAC SRs].SE_NCI
            ORDER BY count DESC
            """);

    [Fact]
    public void Issue2()
        => AssertExecuterSql.Equal(
            "SELECT CAST(\"Creation Date\" AS DATE) AS date, COUNT(*) AS count FROM \"TAC SRs\" GROUP BY CAST(\"Creation Date\" AS DATE) ORDER BY date",
            """
            SELECT
                CAST([TAC SRs].SE_CRE AS DATE) AS [date],
                COUNT(*) AS count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY CAST([TAC SRs].SE_CRE AS DATE)
            ORDER BY [date]
            """);

    [Fact]
    public void Issue3()
        => AssertExecuterSql.Equal(
            "SELECT [Initial Sev.] AS severity, COUNT(*) AS count FROM [TAC SRs] GROUP BY [Initial Sev.] ORDER BY count DESC",
            """
            SELECT
                [TAC SRs].SE_ITI AS severity,
                COUNT(*) AS count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY [TAC SRs].SE_ITI
            ORDER BY count DESC
            """);

    [Fact]
    public void Issue4()
        => AssertExecuterSql.Equal(
            """
            SELECT
                YEAR([Creation Date]) AS Year,
                DATEPART(quarter, [Creation Date]) AS QuarterNum,
                COUNT(*) AS TotalSRs,
                AVG([FResolution Time (h)]) AS AvgResolutionHours
            FROM [TAC SRs]
            GROUP BY YEAR([Creation Date]), DATEPART(quarter, [Creation Date])
            ORDER BY YEAR([Creation Date]), DATEPART(quarter, [Creation Date])
            """,
            """
            SELECT
                YEAR([TAC SRs].SE_CRE) AS Year,
                DATEPART(quarter, [TAC SRs].SE_CRE) AS QuarterNum,
                COUNT(*) AS TotalSRs,
                AVG([TAC SRs].SE_FSO) AS AvgResolutionHours
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY
                YEAR([TAC SRs].SE_CRE),
                DATEPART(quarter, [TAC SRs].SE_CRE)
            ORDER BY
                YEAR([TAC SRs].SE_CRE),
                DATEPART(quarter, [TAC SRs].SE_CRE)
            """);

    [Fact]
    public void Issue5()
        => AssertExecuterSql.Equal(
            "SELECT YEAR([Creation Date]) AS Year, DATEPART(quarter, [Creation Date]) AS QuarterNum, COUNT(*) AS TotalSRs, AVG([FResolution Time (h)]) AS AvgResolutionHours FROM [TAC SRs] GROUP BY YEAR([Creation Date]), DATEPART(quarter, [Creation Date]) ORDER BY YEAR([Creation Date]), DATEPART(quarter, [Creation Date])",
            """
            SELECT
                YEAR([TAC SRs].SE_CRE) AS Year,
                DATEPART(quarter, [TAC SRs].SE_CRE) AS QuarterNum,
                COUNT(*) AS TotalSRs,
                AVG([TAC SRs].SE_FSO) AS AvgResolutionHours
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY
                YEAR([TAC SRs].SE_CRE),
                DATEPART(quarter, [TAC SRs].SE_CRE)
            ORDER BY
                YEAR([TAC SRs].SE_CRE),
                DATEPART(quarter, [TAC SRs].SE_CRE)
            """);

    // "Shipped Date" does not exist in  "Shipments & Returns" should generate a binding error where it can't find the column in the table
    [Fact]
    public void Issue6()
        => AssertExecuterSql.Errors(
            """
            SELECT EXTRACT(YEAR FROM "Shipped Date") AS year,
                    EXTRACT(QUARTER FROM "Shipped Date") AS quarter,
                    EXTRACT(YEAR FROM "Shipped Date") || '-Q' || EXTRACT(QUARTER FROM "Shipped Date") AS year_quarter,
                    SUM("Shipped Quantity") AS shipped_qty
            FROM "Shipments & Returns"
            GROUP BY EXTRACT(YEAR FROM "Shipped Date"), EXTRACT(QUARTER FROM "Shipped Date")
            ORDER BY year, quarter
            """,
            "ColumnNotFound: [25:12] : Column 'Shipped Date' not found in the current scope",
            "ColumnNotFound: [79:12] : Column 'Shipped Date' not found in the current scope",
            "ColumnNotFound: [133:12] : Column 'Shipped Date' not found in the current scope",
            "ColumnNotFound: [181:12] : Column 'Shipped Date' not found in the current scope",
            "ColumnNotFound: [315:12] : Column 'Shipped Date' not found in the current scope",
            "ColumnNotFound: [353:12] : Column 'Shipped Date' not found in the current scope"
            );

    [Fact]
    public void Issue7()
        => AssertExecuterSql.Equal(
            """
            SELECT EXTRACT(YEAR FROM ac."Shipped Date") AS year,
                   EXTRACT(QUARTER FROM ac."Shipped Date") AS quarter,
                   EXTRACT(YEAR FROM ac."Shipped Date") || '-Q' || EXTRACT(QUARTER FROM ac."Shipped Date") AS year_quarter,
                   SUM(sr."Shipped Quantity") AS shipped_qty
            FROM "Shipments & Returns" sr
            JOIN "All Components" ac ON sr."Serial Number" = ac."Serial Number"
            GROUP BY EXTRACT(YEAR FROM ac."Shipped Date"), EXTRACT(QUARTER FROM ac."Shipped Date")
            ORDER BY year, quarter
            """,
            """
            SELECT
                DATEPART(YEAR, ac.PH_SHI) AS year,
                DATEPART(QUARTER, ac.PH_SHI) AS quarter,
                CONCAT(DATEPART(YEAR, ac.PH_SHI), '-Q', DATEPART(QUARTER, ac.PH_SHI)) AS year_quarter,
                SUM(sr.MA_SHI) AS shipped_qty
            FROM landscapeQuery_strategy_A.MA AS sr
            JOIN landscapeQuery_strategy_A.PH AS ac
                ON sr.MA_SER = ac.PH_SER
            GROUP BY
                DATEPART(YEAR, ac.PH_SHI),
                DATEPART(QUARTER, ac.PH_SHI)
            ORDER BY
                year,
                quarter
            """);

    [Fact]
    public void Issue7_2()
        => AssertExecuterSql.Equal(
            """
            SELECT EXTRACT(YEAR FROM ac."Shipped Date") AS year,
                    EXTRACT(QUARTER FROM ac."Shipped Date") AS quarter,
                    CONCAT(EXTRACT(YEAR FROM ac."Shipped Date"), '-Q', EXTRACT(QUARTER FROM ac."Shipped Date")) AS year_quarter,
                    SUM(sr."Shipped Quantity") AS shipped_qty
            FROM "Shipments & Returns" sr
            JOIN "All Components" ac ON sr."Serial Number" = ac."Serial Number"
            GROUP BY EXTRACT(YEAR FROM ac."Shipped Date"), EXTRACT(QUARTER FROM ac."Shipped Date")
            ORDER BY year, quarter
            """,
            """
            SELECT
                DATEPART(YEAR, ac.PH_SHI) AS year,
                DATEPART(QUARTER, ac.PH_SHI) AS quarter,
                CONCAT(DATEPART(YEAR, ac.PH_SHI), '-Q', DATEPART(QUARTER, ac.PH_SHI)) AS year_quarter,
                SUM(sr.MA_SHI) AS shipped_qty
            FROM landscapeQuery_strategy_A.MA AS sr
            JOIN landscapeQuery_strategy_A.PH AS ac
                ON sr.MA_SER = ac.PH_SER
            GROUP BY
                DATEPART(YEAR, ac.PH_SHI),
                DATEPART(QUARTER, ac.PH_SHI)
            ORDER BY
                year,
                quarter
            """);

    [Fact]
    public void Issue7_StringConcat()
        => AssertExecuterSql.Equal(
            """
            SELECT EXTRACT(YEAR FROM ac."Shipped Date") AS year,
                    EXTRACT(QUARTER FROM ac."Shipped Date") AS quarter,
                    EXTRACT(YEAR FROM ac."Shipped Date") || '-Q' || EXTRACT(QUARTER FROM ac."Shipped Date") AS year_quarter,
                    SUM(sr."Shipped Quantity") AS shipped_qty
            FROM "Shipments & Returns" sr
            JOIN "All Components" ac ON sr."Serial Number" = ac."Serial Number"
            GROUP BY EXTRACT(YEAR FROM ac."Shipped Date"), EXTRACT(QUARTER FROM ac."Shipped Date")
            ORDER BY year, quarter
            """,
            """
            SELECT
                DATEPART(YEAR, ac.PH_SHI) AS year,
                DATEPART(QUARTER, ac.PH_SHI) AS quarter,
                CONCAT(DATEPART(YEAR, ac.PH_SHI), '-Q', DATEPART(QUARTER, ac.PH_SHI)) AS year_quarter,
                SUM(sr.MA_SHI) AS shipped_qty
            FROM landscapeQuery_strategy_A.MA AS sr
            JOIN landscapeQuery_strategy_A.PH AS ac
                ON sr.MA_SER = ac.PH_SER
            GROUP BY
                DATEPART(YEAR, ac.PH_SHI),
                DATEPART(QUARTER, ac.PH_SHI)
            ORDER BY
                year,
                quarter
            """);

    [Fact]
    public void Issue8()
        => AssertExecuterSql.Equal(
            """
            SELECT DISTINCT [Assigned Technology] FROM [TAC SRs] WHERE ([Assigned Technology] IS NOT NULL)
            """,
            """
            SELECT DISTINCT [TAC SRs].SE_ASS AS [Assigned Technology]
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE (
                [TAC SRs].SE_ASS IS NOT NULL
            )
            """);

    [Fact]
    public void Issue9()
        => AssertExecuterSql.Equal(
            """
            SELECT TOP 10 * FROM [TAC SRs]
            """,
            """
            SELECT TOP 10
                [TAC SRs].SE_ASS AS [Assigned Technology],
                [TAC SRs].SE_AST AS [Last Update By],
                [TAC SRs].SE_CLO AS [Close Date],
                [TAC SRs].SE_CRE AS [Creation Date],
                [TAC SRs].SE_CUS AS [Customer Incident Number],
                [TAC SRs].SE_ENG AS [Engineer Email],
                [TAC SRs].SE_FSO AS [FResolution Time (h)],
                [TAC SRs].SE_IGN AS [Assigned Product Family],
                [TAC SRs].SE_INC AS [SR Number],
                [TAC SRs].SE_ISO AS [ISolution Time (h)],
                [TAC SRs].SE_ITI AS [Initial Sev.],
                [TAC SRs].SE_LAS AS [Last Updated],
                [TAC SRs].SE_NCI AS [Incident Status],
                [TAC SRs].SE_NIT AS [Initial Response Time (m)],
                [TAC SRs].SE_NTR AS [Contract Number],
                [TAC SRs].SE_ONT AS [Contact Name],
                [TAC SRs].SE_OUT AS [Outage Indicator],
                [TAC SRs].SE_PRO AS [Problem Code],
                [TAC SRs].SE_RES AS [Resolution Code],
                [TAC SRs].SE_SRS AS [SR Summary],
                [TAC SRs].SE_SSI AS [Assigned Sub Technology],
                [TAC SRs].SE_TAC AS [Contact Email],
                [TAC SRs].SE_TIM AS [Time to Close (Days)],
                [TAC SRs].SE_URR AS [Current Sev.],
                [TAC SRs].SE_XIM AS [Maximum Sev.]
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue10()
        => AssertExecuterSql.Equal(
            """
            SELECT TOP 10 [Initial Sev.] FROM [TAC SRs]
            """,
            """
            SELECT TOP 10 [TAC SRs].SE_ITI AS [Initial Sev.]
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue10WithAlias()
        => AssertExecuterSql.Equal(
            """
            SELECT TOP 10 [Initial Sev.] as bar FROM [TAC SRs]
            """,
            """
            SELECT TOP 10 [TAC SRs].SE_ITI AS bar
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue11()
        => AssertExecuterSql.Equal(
            """
            SELECT Count(DISTINCT [Assigned Technology]) FROM [TAC SRs]
            """,
            """
            SELECT COUNT(DISTINCT [TAC SRs].SE_ASS)
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue11_all()
        => AssertExecuterSql.Equal(
            """
            SELECT Count(ALL [Assigned Technology]) FROM [TAC SRs]
            """,
            """
            SELECT COUNT(ALL [TAC SRs].SE_ASS)
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue11_none()
        => AssertExecuterSql.Equal(
            """
            SELECT Count([Assigned Technology]) FROM [TAC SRs]
            """,
            """
            SELECT COUNT([TAC SRs].SE_ASS)
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            """);

    [Fact]
    public void Issue12()
        => AssertExecuterSql
        .WithParameters("selectedTech", "selectedFamily", "selectedStatus")
        .Equal(
            """
            SELECT TOP 10
                [Assigned Product Family],
                COUNT(*) as SR_Count
            FROM [TAC SRs]
            WHERE (@selectedTech = 'All' OR [Assigned Technology] = @selectedTech)
                AND (@selectedFamily = 'All' OR [Assigned Product Family] = @selectedFamily)
                AND (@selectedStatus = 'All' OR [Incident Status] = @selectedStatus)
            GROUP BY [Assigned Product Family]

            UNION ALL

            SELECT
                'Other' AS [Assigned Product Family],
                SUM(SR_Count)
            FROM (
                SELECT COUNT(*) as SR_Count
                FROM [TAC SRs]
                WHERE (@selectedTech = 'All' OR [Assigned Technology] = @selectedTech)
                    AND (@selectedFamily = 'All' OR [Assigned Product Family] = @selectedFamily)
                    AND (@selectedStatus = 'All' OR [Incident Status] = @selectedStatus)
                GROUP BY [Assigned Product Family]
                ORDER BY SR_Count DESC OFFSET 10 ROWS
            ) AS Others
            """,
            """
            SELECT TOP 10
                [TAC SRs].SE_IGN AS [Assigned Product Family],
                COUNT(*) AS SR_Count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE
                (
                    @selectedTech = 'All' OR
                    [TAC SRs].SE_ASS = @selectedTech
                ) AND
                (
                    @selectedFamily = 'All' OR
                    [TAC SRs].SE_IGN = @selectedFamily
                ) AND
                (
                    @selectedStatus = 'All' OR
                    [TAC SRs].SE_NCI = @selectedStatus
                )
            GROUP BY [TAC SRs].SE_IGN
            UNION ALL
            SELECT
                'Other' AS [Assigned Product Family],
                SUM(SR_Count)
            FROM (
                SELECT COUNT(*) AS SR_Count
                FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
                WHERE
                    (
                        @selectedTech = 'All' OR
                        [TAC SRs].SE_ASS = @selectedTech
                    ) AND
                    (
                        @selectedFamily = 'All' OR
                        [TAC SRs].SE_IGN = @selectedFamily
                    ) AND
                    (
                        @selectedStatus = 'All' OR
                        [TAC SRs].SE_NCI = @selectedStatus
                    )
                GROUP BY [TAC SRs].SE_IGN
                ORDER BY SR_Count DESC OFFSET 10 ROWS
            ) AS Others
            """);

    [Fact]
    public void Issue13()
        => AssertExecuterSql
        .WithParameters("selectedTech", "selectedFamily", "selectedStatus")
        .Errors(
            """
            SELECT TOP 10 [Assigned Product Family], COUNT(*) as SR_Count FROM[TAC SRs] WHERE(@selectedTech = 'All' OR[Assigned Technology] = @selectedTech) AND(@selectedFamily = 'All' OR[Assigned Product Family] = @selectedFamily) AND(@selectedStatus = 'All' OR[Incident Status] = @selectedStatus) GROUP BY[Assigned Product Family] ORDER BY SR_Count DESC UNION ALL SELECT 'Other' AS[Assigned Product Family], SUM(SR_Count) FROM(SELECT COUNT(*) as SR_Count FROM [TAC SRs] WHERE (@selectedTech = 'All' OR[Assigned Technology] = @selectedTech) AND(@selectedFamily = 'All' OR[Assigned Product Family] = @selectedFamily) AND(@selectedStatus = 'All' OR[Incident Status] = @selectedStatus) GROUP BY[Assigned Product Family] ORDER BY SR_Count DESC OFFSET 10 ROWS) AS Others
            """,
            "UnexpectedToken: [344:9] : 'UNION' was unexpected");

    [Fact]
    public void Issue14()
        => AssertExecuterSql
            .WithParameters("selectedTech", "selectedFamily", "selectedStatus")
            .Equal(
            """
            SELECT [Assigned Product Family], COUNT(*) AS SR_Count
            FROM [TAC SRs]
            WHERE (@selectedTech = 'All' OR [Assigned Technology] = @selectedTech)
              AND (@selectedFamily = 'All' OR [Assigned Product Family] = @selectedFamily)
              AND (@selectedStatus = 'All' OR [Incident Status] = @selectedStatus)
            GROUP BY [Assigned Product Family]
            ORDER BY SR_Count DESC
            OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
            """,
            """
            SELECT
                [TAC SRs].SE_IGN AS [Assigned Product Family],
                COUNT(*) AS SR_Count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE
                (
                    @selectedTech = 'All' OR
                    [TAC SRs].SE_ASS = @selectedTech
                ) AND
                (
                    @selectedFamily = 'All' OR
                    [TAC SRs].SE_IGN = @selectedFamily
                ) AND
                (
                    @selectedStatus = 'All' OR
                    [TAC SRs].SE_NCI = @selectedStatus
                )
            GROUP BY [TAC SRs].SE_IGN
            ORDER BY SR_Count DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
            """);
    [Fact]
    public void Issue15()
        => AssertExecuterSql
            .WithParameters("tech", "family", "status")
            .Equal(
            """
               SELECT
               COUNT(*) as open 
               FROM [TAC SRs] 
               WHERE (
                [Incident Status] NOT LIKE 'Closed%' AND 
                [Incident Status] NOT LIKE 'Solution Provided%'
               ) AND (
                 [Assigned Technology] = @tech OR 
                 @tech IS NULL
               ) AND (
                 [Assigned Product Family] = @family OR 
                 @family IS NULL
               ) AND (
                 [Incident Status] = @status OR @status IS NULL
               )
            """,
            """
            SELECT COUNT(*) AS [open]
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE
                (
                    [TAC SRs].SE_NCI NOT LIKE 'Closed%' AND
                    [TAC SRs].SE_NCI NOT LIKE 'Solution Provided%'
                ) AND
                (
                    [TAC SRs].SE_ASS = @tech OR
                    @tech IS NULL
                ) AND
                (
                    [TAC SRs].SE_IGN = @family OR
                    @family IS NULL
                ) AND
                (
                    [TAC SRs].SE_NCI = @status OR
                    @status IS NULL
                )
            """); [Fact]
    public void Issue16()
        => AssertExecuterSql
            .WithParameters("tech", "family", "status")
            .Equal(
            """
              SELECT COUNT(*) as open FROM [TAC SRs] WHERE ([Incident Status] NOT LIKE 'Closed%' AND [Incident Status] NOT LIKE 'Solution Provided%') AND ([Assigned Technology] = @tech OR @tech IS NULL) AND ([Assigned Product Family] = @family OR @family IS NULL) AND ([Incident Status] = @status OR @status IS NULL)
            """,
            """
            SELECT COUNT(*) AS [open]
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE
                (
                    [TAC SRs].SE_NCI NOT LIKE 'Closed%' AND
                    [TAC SRs].SE_NCI NOT LIKE 'Solution Provided%'
                ) AND
                (
                    [TAC SRs].SE_ASS = @tech OR
                    @tech IS NULL
                ) AND
                (
                    [TAC SRs].SE_IGN = @family OR
                    @family IS NULL
                ) AND
                (
                    [TAC SRs].SE_NCI = @status OR
                    @status IS NULL
                )
            """);

    [Fact]
    public void Issue17()
        => AssertExecuterSql
            .WithParameters("start_date")
        .Equal(
            "SELECT [Close Date], AVG([FResolution Time (h)]) AS AvgResolution FROM [TAC SRs] WHERE [Close Date] >= @start_date GROUP BY [Close Date] ORDER BY [Close Date] ASC",
            """
            SELECT
                [TAC SRs].SE_CLO AS [Close Date],
                AVG([TAC SRs].SE_FSO) AS AvgResolution
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE [TAC SRs].SE_CLO >= @start_date
            GROUP BY [TAC SRs].SE_CLO
            ORDER BY [Close Date] ASC
            """);
    [Fact]
    public void Issue18()
        => AssertExecuterSql
            .WithParameters("start_date")
        .Equal(
            "SELECT [Close Date], AVG([FResolution Time (h)]) AS AvgResolution FROM [TAC SRs] WHERE [Close Date] >= @start_date GROUP BY [Close Date] ORDER BY 1 ASC",
            """
            SELECT
                [TAC SRs].SE_CLO AS [Close Date],
                AVG([TAC SRs].SE_FSO) AS AvgResolution
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            WHERE [TAC SRs].SE_CLO >= @start_date
            GROUP BY [TAC SRs].SE_CLO
            ORDER BY 1 ASC
            """);


    [Fact]
    public void Issue19()
        => AssertExecuterSql
            .WithSchema(TestConfigSchema.AnonVirtualSchema)
        .Equal(
            """
            SELECT
            dh.[Historic Month] AS month,
            AVG(dh.[SRs as % of IB]) AS avg_srs_percent,
            CASE WHEN isw.[SW Replaced Date] IS NOT NULL THEN 1 ELSE 0 END AS replaced_flag
            FROM [All Components] ac
            JOIN [EV_PH] evph ON ac._id = evph.PH_id
            JOIN [Device History] dh ON evph.EV_id = dh._id
            LEFT JOIN [OF_PH] ofph ON ac._id = ofph.PH_id
            LEFT JOIN [Installed Software] isw ON ofph.OF_id = isw._id
            WHERE ac.[Device SubType] LIKE '%Router%' GROUP BY dh.[Historic Month], CASE WHEN isw.[SW Replaced Date] IS NOT NULL THEN 1 ELSE 0 END ORDER BY dh.[Historic Month]
            """,
            """
            SELECT
                dh.EV_FRO AS month,
                AVG(dh.EV_SRS) AS avg_srs_percent,
                CASE
                    WHEN isw.OF_SWR IS NOT NULL THEN 1
                    ELSE 0
                END AS replaced_flag
            FROM landscapeQuery_strategy_A.PH AS ac
            JOIN landscapeQuery_strategy_A.EV_PH AS evph
                ON ac.PHId = evph.PHId
            JOIN landscapeQuery_strategy_A.EV AS dh
                ON evph.EVId = dh.EVId
            LEFT JOIN landscapeQuery_strategy_A.OF_PH AS ofph
                ON ac.PHId = ofph.PHId
            LEFT JOIN landscapeQuery_strategy_A.[OF] AS isw
                ON ofph.OFId = isw.OFId
            WHERE ac.PH_DEV LIKE '%Router%'
            GROUP BY
                dh.EV_FRO,
                CASE
                    WHEN isw.OF_SWR IS NOT NULL THEN 1
                    ELSE 0
                END
            ORDER BY dh.EV_FRO
            """);
    [Fact]
    public void OrderByColumnAlias()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SchemaVirtualSchema)
        .Equal(
            """
            SELECT
                CONVERT(date, SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY
                CONVERT(date, SE_CRE)
            ORDER BY month
            """,
            """
            SELECT
                CONVERT([date], SE.SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM landscapeQuery_strategy_A.SE AS SE
            GROUP BY CONVERT([date], SE.SE_CRE)
            ORDER BY month
            """.Trim());

    [Fact]
    public void OrderByColumnOrdinal()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SchemaVirtualSchema)
        .Equal(
            """
            SELECT
                CONVERT(date, SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY
                CONVERT(date, SE_CRE)
            ORDER BY 1
            """,
            """
            SELECT
                CONVERT([date], SE.SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM landscapeQuery_strategy_A.SE AS SE
            GROUP BY CONVERT([date], SE.SE_CRE)
            ORDER BY 1
            """.Trim());

    [Fact]
    public void Test1()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SchemaVirtualSchema)
        .Equal(
            "SELECT BE_BPR_Alias FROM BE",
            """
            SELECT BE.BE_BPR AS BE_BPR_Alias
            FROM landscapeQuery_strategy_A.BE AS BE
            """.Trim());

    [Fact]
    public void SubQueryReplace()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SchemaVirtualSchema)
        .Equal(
            "SELECT Col1, Col2 FROM filter_Table",
            """
            SELECT
                filter_Table.AAA AS Col1,
                filter_Table.BBB AS Col2
            FROM (
                SELECT
                    AAA,
                    BBB,
                    CCC
                FROM FOO
                WHERE AAA = 'test'
            ) AS filter_Table
            """.Trim());

    [Fact]
    public void ComplexExpression()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SchemaVirtualSchema)
        .Equal(
            "SELECT Concat(Col1, Col2) as bar FROM filter_Table",
            """
            SELECT CONCAT(filter_Table.AAA, filter_Table.BBB) AS bar
            FROM (
                SELECT
                    AAA,
                    BBB,
                    CCC
                FROM FOO
                WHERE AAA = 'test'
            ) AS filter_Table
            """.Trim());

    [Fact]
    public void ColumnInvalidAsNotInGroupByOrAnAgrgegate()
        => AssertExecuterSql
        .Errors("""
            WITH counted AS (
              SELECT [Defect Reported Version] AS version,
                     COUNT(*) AS cnt,
                     ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn
              FROM [Defects]
              GROUP BY [Defect Reported Version]
            )
            SELECT CASE WHEN rn <= 10 THEN version ELSE 'Other' END AS version,
                   SUM(cnt) AS cnt
            FROM counted
            GROUP BY CASE WHEN rn <= 10 THEN version ELSE 'Other' END
            ORDER BY CASE WHEN version = 'Other' THEN 1 ELSE 0 END, cnt DESC
            """,
            """
            InvalidColumnInOrderBy: [376:45] : Expression 'counted.version' in ORDER BY must be either an aggregate or a grouped column.
            """);
}
