namespace Tabliq.Tests.Execution.SqlServer.Issues;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/24
/// </summary>
public class Issue24
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .WithParameters("search_term")
        .Equal(
            """
            SELECT COUNT(DISTINCT ba._id) AS matching_business_application_count
            FROM [Business Applications] ba
            WHERE UPPER(COALESCE(ba._id, '')) LIKE '%' || UPPER(@search_term) || '%'
               OR UPPER(COALESCE(ba.Name, '')) LIKE '%' || UPPER(@search_term) || '%'
            """,
            """
            SELECT COUNT(DISTINCT ba.TRId) AS matching_business_application_count
            FROM landscapeQuery_strategy_A.TR AS ba
            WHERE
                UPPER(COALESCE(ba.TRId, '')) LIKE CONCAT('%', UPPER(@search_term), '%') OR
                UPPER(COALESCE(ba.TR_UID, '')) LIKE CONCAT('%', UPPER(@search_term), '%')
            """);
}
