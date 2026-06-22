using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
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
                CAST([TAC SRs].SE_CRE AS DATE) AS date,
                COUNT(*) AS count
            FROM landscapeQuery_strategy_A.SE AS [TAC SRs]
            GROUP BY CAST([TAC SRs].SE_CRE AS DATE)
            ORDER BY date
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
    //[Fact]
    //public void Issue6()
    //    => AssertExecuterSql.Errors(
    //        """
    //        SELECT EXTRACT(YEAR FROM "Shipped Date") AS year,
    //                EXTRACT(QUARTER FROM "Shipped Date") AS quarter,
    //                EXTRACT(YEAR FROM "Shipped Date") || '-Q' || EXTRACT(QUARTER FROM "Shipped Date") AS year_quarter,
    //                SUM("Shipped Quantity") AS shipped_qty
    //        FROM "Shipments & Returns"
    //        GROUP BY EXTRACT(YEAR FROM "Shipped Date"), EXTRACT(QUARTER FROM "Shipped Date")
    //        ORDER BY year, quarter
    //        """);

    //[Fact]
    //public void Issue7()
    //    => AssertSqlDoeNotGenerate(
    //        TestSchema.SchemaFriendlyNames,
    //        """
    //        SELECT EXTRACT(YEAR FROM ac."Shipped Date") AS year,
    //               EXTRACT(QUARTER FROM ac."Shipped Date") AS quarter,
    //               EXTRACT(YEAR FROM ac."Shipped Date") || '-Q' || EXTRACT(QUARTER FROM ac."Shipped Date") AS year_quarter,
    //               SUM(sr."Shipped Quantity") AS shipped_qty
    //        FROM "Shipments & Returns" sr
    //        JOIN "All Components" ac ON sr."Serial Number" = ac."Serial Number"
    //        GROUP BY EXTRACT(YEAR FROM ac."Shipped Date"), EXTRACT(QUARTER FROM ac."Shipped Date")
    //        ORDER BY year, quarter
    //        """);

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
            ORDER BY SR_Count DESC

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
            ORDER BY SR_Count DESC
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
                CONVERT(date, SE.SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM landscapeQuery_strategy_A.SE AS SE
            GROUP BY CONVERT(date, SE.SE_CRE)
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
                CONVERT(date, SE.SE_CRE) AS month,
                COUNT(*) AS incident_count
            FROM landscapeQuery_strategy_A.SE AS SE
            GROUP BY CONVERT(date, SE.SE_CRE)
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
}
