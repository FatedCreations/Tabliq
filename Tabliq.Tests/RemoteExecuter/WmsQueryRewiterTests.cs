namespace Tabliq.Tests.RemoteExecuter;

public class WmsQueryRewiterTests
{
    [Fact]
    public void Issue1()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.WmsVirtualSchema)
        .Equal(
            """
            SELECT [System Name], [Manufacturer], [Model], [EOL / End of Extended Support Date] FROM [Server Assets] WHERE [EOL Status] = 'EOL' OR [EOL / End of Extended Support Date] <= DATEADD(year, 2, GETDATE()) ORDER BY [EOL / End of Extended Support Date] ASC
            """,
            """
            SELECT
                [Server Assets].UB_UID AS [System Name],
                [Server Assets].UB_MAN AS Manufacturer,
                [Server Assets].UB_MOD AS Model,
                [Server Assets].UB_OLE AS [EOL / End of Extended Support Date]
            FROM landscapeQuery_strategy_A.UB AS [Server Assets]
            WHERE
                [Server Assets].UB_EOL = 'EOL' OR
                [Server Assets].UB_OLE <= DATEADD(year, 2, GETDATE())
            ORDER BY [EOL / End of Extended Support Date] ASC
            """);

}
