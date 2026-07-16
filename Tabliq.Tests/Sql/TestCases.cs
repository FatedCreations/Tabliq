using Xunit;
using Tabliq.Sql.Core;
using Tabliq.Sql.Ast;
using Tabliq.Sql.Parsing;

namespace Tabliq.Tests.Sql;

public class TestCases
{

    [Fact]
    public void SemiColonStatemntSeperator()
        => AssertSql
            .SkipBinder()
            .Equal(
                """
                SELECT * FROM landscapeQuery_strategy_A.BE;
                """,
                """
                SELECT *
                FROM landscapeQuery_strategy_A.BE;
                """);

    [Fact]
    public void SemiColonStatemntSeperatorWithEmpty()
        => AssertSql
            .SkipBinder()
            .Equal(
                """
                SELECT * FROM landscapeQuery_strategy_A.BE;;
                """,
                """
                SELECT *
                FROM landscapeQuery_strategy_A.BE;;
                """);
    [Fact]
    public void Between()
        => AssertSql
            .SkipBinder()
            .Equal(
                """
                SELECT * FROM landscapeQuery_strategy_A.BE WHERE BEId between 100 and 200
                """,
                """
                SELECT *
                FROM landscapeQuery_strategy_A.BE
                WHERE BEId BETWEEN 100 AND 200
                """);

    [Fact]
    public void ComplexTableName()
        => AssertSql
            .SkipBinder()
            .Equal(
                """
                SELECT * FROM landscapeQuery_strategy_A.BE
                """,
                """
                SELECT *
                FROM landscapeQuery_strategy_A.BE
                """);

    [Fact]
    public void AmbiguousColumnName()
        => AssertSql.WithErrors(
            """
            SELECT 
                [ODId]
                FROM OD_PR
                join OD on OD_PR.ODId = OD.ODId
            """,
            "AmbiguousColumn: [12:4] : Column 'ODId' is ambiguous in the current scope");
    [Fact]
    public void OrderByAliasColumns()
        => AssertSql.Equal(
            """
            SELECT SE_CRE AS d
            FROM SE 
            ORDER BY d
            """,
            """
            SELECT SE_CRE AS d
            FROM SE
            ORDER BY d
            """);

    [Fact]
    public void GroupByAliasColumns()
        => AssertSql.WithErrors (
            """
            SELECT SE_CRE AS d
            FROM SE 
            GROUP BY d
            """,
            "ColumnNotFound: [37:1] : Column 'd' not found in the current scope"
            );

    [Fact]
    public void ConvertExpressionWithDataTypeKeywords()
        => AssertSql.Equal(
            """
            SELECT CONVERT(date, DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1)) AS month, COUNT(*) AS incident_count FROM SE GROUP BY YEAR(SE_CRE), MONTH(SE_CRE) ORDER BY month
            """,
            """
            SELECT
                CONVERT([date], DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1)) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY
                YEAR(SE_CRE),
                MONTH(SE_CRE)
            ORDER BY month
            """);

    [Fact]
    public void CastExpressionWithDataTypeKeywords()
        => AssertSql.Equal(
            """
            SELECT Cast(DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1) as date) AS month, COUNT(*) AS incident_count FROM SE GROUP BY YEAR(SE_CRE), MONTH(SE_CRE) ORDER BY month
            """,
            """
            SELECT
                CAST(DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1) AS date) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY
                YEAR(SE_CRE),
                MONTH(SE_CRE)
            ORDER BY month
            """);
    [Fact]
    public void CastExpression()
        => AssertSql.Equal(
            """
            SELECT Cast(SE_CRE as date) AS d FROM SE
            """,
            """
            SELECT CAST(SE_CRE AS date) AS d
            FROM SE
            """);

    [Fact]
    public void ExtractFunctionFromSyntax()
        => AssertSql.Equal(
            """
            SELECT EXTRACT(YEAR FROM SE_CRE) FROM SE
            """,
            """
            SELECT EXTRACT(YEAR FROM SE_CRE)
            FROM SE
            """);

    [Fact]
    public void DatePart()
        => AssertSql.Equal(
            """
            SELECT DatePart(YEAR, SE_CRE) FROM SE
            """,
            """
            SELECT DATEPART(YEAR, SE_CRE)
            FROM SE
            """);

    [Fact]
    public void PArseExpressionWithDataTypeKeywords()
        => AssertSql.Equal(
            """
            SELECT Parse(DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1) as date) AS month, COUNT(*) AS incident_count FROM SE GROUP BY YEAR(SE_CRE), MONTH(SE_CRE) ORDER BY month
            """,
            """
            SELECT
                PARSE(DATEFROMPARTS(YEAR(SE_CRE), MONTH(SE_CRE), 1) AS date) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY
                YEAR(SE_CRE),
                MONTH(SE_CRE)
            ORDER BY month
            """);

    [Fact]
    public void ParseExpressionWithDataTypeKeywords()
        => AssertSql.Equal(
            """
            SELECT Parse(SE_CRE as date) AS month, COUNT(*) AS incident_count FROM SE GROUP BY PARSE(SE_CRE AS date) ORDER BY month
            """,
            """
            SELECT
                PARSE(SE_CRE AS date) AS month,
                COUNT(*) AS incident_count
            FROM SE
            GROUP BY PARSE(SE_CRE AS date)
            ORDER BY month
            """
            );

    [Fact]
    public void SelectStart()
        => AssertSql.Equal(
            """
            SELECT * FROM BE
            """,
            """
            SELECT *
            FROM BE
            """);

    [Fact]
    public void Simple()
        => AssertSql.Equal(
            """
            SELECT BEId FROM BE
            """,
            """
            SELECT BEId
            FROM BE
            """);

    [Fact]
    public void SelectWithWhereAndJoin()
        => AssertSql.Equal(
            """
            SELECT b.BEId,b.BE_DES,es.ES_BPR FROM BE b JOIN BE_ES be_es ON b.BEId=be_es.BEId JOIN ES es ON be_es.ESId=es.ESId WHERE es.ES_BPR LIKE '%test%' AND b.BE_CRE>='2020-01-01' ORDER BY b.BEId DESC
            """,
            """
            SELECT
                b.BEId,
                b.BE_DES,
                es.ES_BPR
            FROM BE AS b
            JOIN BE_ES AS be_es
                ON b.BEId = be_es.BEId
            JOIN ES AS es
                ON be_es.ESId = es.ESId
            WHERE
                es.ES_BPR LIKE '%test%' AND
                b.BE_CRE >= '2020-01-01'
            ORDER BY b.BEId DESC
            """);

    [Fact]
    public void GroupByWithHaving()
        => AssertSql.Equal(
            """
            SELECT OF_UID,COUNT(*) AS Orders,AVG(OF_SWR) avgSWR FROM [OF] GROUP BY OF_UID HAVING COUNT(*)>5
            """,
            """
            SELECT
                OF_UID,
                COUNT(*) AS Orders,
                AVG(OF_SWR) AS avgSWR
            FROM [OF]
            GROUP BY OF_UID
            HAVING COUNT(*) > 5
            """);

    [Fact]
    public void WindowFunctionRowNumber()
        => AssertSql.Equal(
            """
            SELECT OFId,OF_UID,ROW_NUMBER()OVER(PARTITION BY OF_UID ORDER BY OF_LAS DESC) rn FROM [OF] WHERE OF_LAS IS NOT NULL
            """,
            """
            SELECT
                OFId,
                OF_UID,
                ROW_NUMBER() OVER (PARTITION BY OF_UID ORDER BY OF_LAS DESC) AS rn
            FROM [OF]
            WHERE OF_LAS IS NOT NULL
            """);

    [Fact]
    public void WindowFunctionAggregateOver()
        => AssertSql.Equal(
            """
            SELECT PHId,PH_SER,COUNT(*) OVER (PARTITION BY PH_SER) serCount FROM PH
            """,
            """
            SELECT
                PHId,
                PH_SER,
                COUNT(*) OVER (PARTITION BY PH_SER) AS serCount
            FROM PH
            """);

    [Fact]
    public void CteSimple()
        => AssertSql.Equal(
            """
            WITH recentOrders AS(SELECT OFId,OF_UID,OF_LAS FROM [OF] WHERE OF_LAS>='2023-01-01') SELECT * FROM recentOrders
            """,
            """
            WITH recentOrders AS (
                SELECT
                    OFId,
                    OF_UID,
                    OF_LAS
                FROM [OF]
                WHERE OF_LAS >= '2023-01-01'
            )
            SELECT *
            FROM recentOrders
            """);

    [Fact]
    public void CteRecursiveLikeExample()
        => AssertSql.Equal(
            """
            WITH cte AS(SELECT ROId,RO_PRI FROM RO WHERE RO_PRI IS NOT NULL UNION ALL SELECT r.ROId,r.RO_PRI FROM RO r JOIN cte ON r.RO_PRI=cte.RO_PRI) SELECT TOP 10 * FROM cte
            """,
            """
            WITH cte AS (
                SELECT
                    ROId,
                    RO_PRI
                FROM RO
                WHERE RO_PRI IS NOT NULL
                UNION ALL
                SELECT
                    r.ROId,
                    r.RO_PRI
                FROM RO AS r
                JOIN cte
                    ON r.RO_PRI = cte.RO_PRI
            )
            SELECT TOP 10 *
            FROM cte
            """);

    [Fact]
    public void SubqueryInSelect()
        => AssertSql.Equal(
            """
            SELECT OFId,(SELECT COUNT(*) FROM OF_SO so WHERE so.OFId=[OF].OFId) as SoCount FROM [OF]
            """,
            """
            SELECT
                OFId,
                (
                    SELECT COUNT(*)
                    FROM OF_SO AS so
                    WHERE so.OFId = [OF].OFId
                ) AS SoCount
            FROM [OF]
            """);

    [Fact]
    public void CorrelatedSubqueryWhereExists()
        => AssertSql.Equal(
            """
            SELECT * FROM [OF] o WHERE EXISTS(SELECT 1 FROM OF_SO so WHERE so.OFId=o.OFId AND so.SOId>100)
            """,
            """
            SELECT *
            FROM [OF] AS o
            WHERE EXISTS (
                SELECT 1
                FROM OF_SO AS so
                WHERE
                    so.OFId = o.OFId AND
                    so.SOId > 100
            )
            """);

    [Fact]
    public void NestedSubqueries()
        => AssertSql.Equal(
            """
            SELECT * FROM (SELECT OFId,OF_UID,(SELECT COUNT(*) FROM OF_SO so WHERE so.OFId=innerq.OFId) cnt FROM [OF] innerq) t WHERE cnt>0
            """,
            """
            SELECT *
            FROM (
                SELECT
                    OFId,
                    OF_UID,
                    (
                        SELECT COUNT(*)
                        FROM OF_SO AS so
                        WHERE so.OFId = innerq.OFId
                    ) AS cnt
                FROM [OF] AS innerq
            ) AS t
            WHERE cnt > 0
            """);

    [Fact(Skip = "Out of scope: DECLARE statements are not supported in query-only parser mode")]
    public void ParametersDeclared()
        => AssertSql.Equal(
            """
            DECLARE @minId bigint = 100;SELECT * FROM [OF] WHERE OFId>@minId
            """,
            """
            DECLARE @minId bigint = 100;
            SELECT *
            FROM [OF]
            WHERE OFId > @minId
            """);

    [Fact]
    public void ParametersInlineInWhere()
        => AssertSql
        .WithParameters("series")
        .Equal(
            """
            SELECT * FROM PH WHERE PH_SER=@series AND PH_LOG=1
            """,
            """
            SELECT *
            FROM PH
            WHERE
                PH_SER = @series AND
                PH_LOG = 1
            """);

    [Fact]
    public void ParamaetersNotBound()
        => AssertSql
            .WithParameters("other")
            .WithErrors(
            """
            SELECT * FROM PH WHERE PH_SER=@series AND PH_LOG=1
            """,
            "ParameterNotFound: [30:7] : Parameter '@series' not provided");

    [Fact]
    public void JoinMultipleTypes()
        => AssertSql.Equal(
            """
            SELECT o.OFId,s.SOId,opr.PRId FROM [OF] o LEFT JOIN OF_SO os ON o.OFId=os.OFId LEFT JOIN SO s ON os.SOId=s.SOId LEFT JOIN OD_PR opr ON opr.PRId=s.SOId JOIN OD od ON od.ODId=opr.ODId
            """,
            """
            SELECT
                o.OFId,
                s.SOId,
                opr.PRId
            FROM [OF] AS o
            LEFT JOIN OF_SO AS os
                ON o.OFId = os.OFId
            LEFT JOIN SO AS s
                ON os.SOId = s.SOId
            LEFT JOIN OD_PR AS opr
                ON opr.PRId = s.SOId
            JOIN OD AS od
                ON od.ODId = opr.ODId
            """);

    [Fact]
    public void JoinInner()
        => AssertSql.Equal(
            """
            SELECT o.OFId FROM [OF] o INNER JOIN OF_SO os ON o.OFId=os.OFId
            """,
            """
            SELECT o.OFId
            FROM [OF] AS o
            INNER JOIN OF_SO AS os
                ON o.OFId = os.OFId
            """);

    [Fact]
    public void JoinLeftOuter()
        => AssertSql.Equal(
            """
            SELECT o.OFId FROM [OF] o LEFT OUTER JOIN OF_SO os ON o.OFId=os.OFId
            """,
            """
            SELECT o.OFId
            FROM [OF] AS o
            LEFT OUTER JOIN OF_SO AS os
                ON o.OFId = os.OFId
            """);

    [Fact]
    public void JoinRightOuter()
        => AssertSql.Equal(
            """
            SELECT o.OFId FROM [OF] o RIGHT OUTER JOIN OF_SO os ON o.OFId=os.OFId
            """,
            """
            SELECT o.OFId
            FROM [OF] AS o
            RIGHT OUTER JOIN OF_SO AS os
                ON o.OFId = os.OFId
            """);

    [Fact]
    public void JoinFullOuter()
        => AssertSql.Equal(
            """
            SELECT o.OFId FROM [OF] o FULL OUTER JOIN OF_SO os ON o.OFId=os.OFId
            """,
            """
            SELECT o.OFId
            FROM [OF] AS o
            FULL OUTER JOIN OF_SO AS os
                ON o.OFId = os.OFId
            """);

    [Fact]
    public void JoinCross()
        => AssertSql.Equal(
            """
            SELECT o.OFId FROM [OF] o CROSS JOIN OF_SO os
            """,
            """
            SELECT o.OFId
            FROM [OF] AS o
            CROSS JOIN OF_SO AS os
            """);

    [Fact]
    public void ComplexExpressionsAndCase()
        => AssertSql.Equal(
            """
            SELECT PHId,CASE WHEN PH_NEW>0 THEN 'new' ELSE 'old' END as AgeBucket,PH_NEW*1.0/NULLIF(PH_SHI,0) ratio FROM PH
            """,
            """
            SELECT
                PHId,
                CASE
                    WHEN PH_NEW > 0 THEN 'new'
                    ELSE 'old'
                END AS AgeBucket,
                PH_NEW * 1.0 / NULLIF(PH_SHI, 0) AS ratio
            FROM PH
            """);

    [Fact]
    public void Case()
        => AssertSql.Equal(
            """
            SELECT PHId,CASE WHEN PH_NEW>0 THEN 'new' ELSE 'old' END as AgeBucket FROM PH
            """,
            """
            SELECT
                PHId,
                CASE
                    WHEN PH_NEW > 0 THEN 'new'
                    ELSE 'old'
                END AS AgeBucket
            FROM PH
            """);

    [Fact]
    public void UnionAllAndOrder()
        => AssertSql.Equal(
            """
            SELECT BEId AS Id,'BE' t FROM BE UNION ALL SELECT ESId,'ES' FROM ES ORDER BY 1
            """,
            """
            SELECT
                BEId AS Id,
                'BE' AS t
            FROM BE
            UNION ALL
            SELECT
                ESId,
                'ES'
            FROM ES
            ORDER BY 1
            """);

    [Fact]
    public void UnionAndOrder()
        => AssertSql.Equal(
            """
            SELECT BEId AS Id,'BE' t FROM BE UNION SELECT ESId,'ES' FROM ES ORDER BY 1
            """,
            """
            SELECT
                BEId AS Id,
                'BE' AS t
            FROM BE
            UNION
            SELECT
                ESId,
                'ES'
            FROM ES
            ORDER BY 1
            """);

    [Fact]
    public void ExistsAndNotExists()
        => AssertSql.Equal(
            """
            SELECT * FROM MA m WHERE EXISTS(SELECT 1 FROM MA_RM rm WHERE rm.MAId=m.MAId) AND NOT EXISTS(SELECT 1 FROM MA_OD od WHERE od.MAId=m.MAId)
            """,
            """
            SELECT *
            FROM MA AS m
            WHERE
                EXISTS (
                    SELECT 1
                    FROM MA_RM AS rm
                    WHERE rm.MAId = m.MAId
                ) AND
                NOT EXISTS (
                    SELECT 1
                    FROM MA_OD AS od
                    WHERE od.MAId = m.MAId
                )
            """);

    [Fact]
    public void NotExists()
        => AssertSql.Equal(
            """
            SELECT * FROM MA m WHERE NOT EXISTS(SELECT 1 FROM MA_OD od WHERE od.MAId=m.MAId)
            """,
            """
            SELECT *
            FROM MA AS m
            WHERE NOT EXISTS (
                SELECT 1
                FROM MA_OD AS od
                WHERE od.MAId = m.MAId
            )
            """);
    [Fact]
    public void Not()
        => AssertSql.Equal(
            """
            SELECT * FROM MA m WHERE NOT (1 = 1)
            """,
            """
            SELECT *
            FROM MA AS m
            WHERE NOT (
                1 = 1
            )
            """);

    [Fact(Skip = "Out of scope: INSERT statements are not supported in query-only parser mode")]
    public void InsertSelect()
        => AssertSql.Equal(
            """
            INSERT INTO NE(NEId,NE_INI) SELECT NEWID(),NE_INI FROM NE WHERE NE_INI IS NOT NULL
            """,
            """
            INSERT INTO NE (NEId, NE_INI)
            SELECT NEWID(),
                    NE_INI
            FROM NE
            WHERE NE_INI IS NOT NULL
            """);

    [Fact(Skip = "Out of scope: UPDATE statements are not supported in query-only parser mode")]
    public void UpdateFromWithJoin()
        => AssertSql.Equal(
            """
            UPDATE o SET OF_TRA=s.SO_DAT FROM [OF] o JOIN OF_SO os ON o.OFId=os.OFId JOIN SO s ON os.SOId=s.SOId WHERE s.SO_DAT>'2022-01-01'
            """,
            """
            UPDATE o
            SET OF_TRA = s.SO_DAT
            FROM [OF] AS o
            JOIN OF_SO AS os
                ON o.OFId = os.OFId
            JOIN SO AS s
                ON os.SOId = s.SOId
            WHERE s.SO_DAT > '2022-01-01'
            """);

    [Fact(Skip = "Out of scope: DELETE statements are not supported in query-only parser mode")]
    public void DeleteWithWhereExists()
        => AssertSql.Equal(
            """
            DELETE FROM [OF] WHERE NOT EXISTS(SELECT 1 FROM OF_SO so WHERE so.OFId=OF.OFId)
            """,
            """
            DELETE
            FROM [OF]
            WHERE NOT EXISTS (
                SELECT 1
                FROM OF_SO AS so
                WHERE so.OFId = OF.OFId
            )
            """);

    [Fact(Skip = "Out of scope: Window functions with RANGE clauses are not currently supported")]
    public void ComplexWindowPartitionOrderRange()
        => AssertSql.Equal(
            """
            SELECT EVId,EV_FRO, SUM(EV_TOT) OVER(PARTITION BY EV_UID ORDER BY EV_FRO RANGE BETWEEN INTERVAL '7' DAY PRECEDING AND CURRENT ROW) weeklySum FROM EV
            """,
            """
            SELECT
                EVId,
                EV_FRO,
                SUM(EV_TOT) OVER (
                    PARTITION BY EV_UID
                    ORDER BY EV_FRO
                    RANGE BETWEEN INTERVAL '7' DAY PRECEDING AND CURRENT ROW
                ) AS weeklySum
            FROM EV
            """);

    [Fact]
    public void GroupingSetsRollupCubeExamples()
        => AssertSql.Equal(
            """
            SELECT OF_UID,OF_OFT,COUNT(*) FROM [OF] GROUP BY ROLLUP(OF_UID,OF_OFT)
            """,
            """
            SELECT
                OF_UID,
                OF_OFT,
                COUNT(*)
            FROM [OF]
            GROUP BY ROLLUP(OF_UID, OF_OFT)
            """);

    [Fact]
    public void ParseExtractFunction()
        => AssertSql.Equal(
            """
            SELECT EXTRACT(quarter  from SE_CRE) as quarter FROM SE
            """,
            """
            SELECT EXTRACT(quarter FROM SE_CRE) AS quarter
            FROM SE
            """);

    [Fact]
    public void ConcateOp()
        => AssertSql.Equal(
            """
            SELECT SE_CRE || '-' || SE_CRE as quarter FROM SE
            """,
            """
            SELECT SE_CRE || '-' || SE_CRE AS quarter
            FROM SE
            """);
}