
namespace Tabliq.Tests.RemoteExecuter;

/// <summary>
/// https://github.com/FatedCreations/Tabliq/issues/20
/// </summary>
public class Issue20
{
    [Fact]
    public void Test()
        => AssertExecuterSql
        .WithSchema(TestConfigSchema.SlaVirtualSchema)
        .Equal(
            """
             SELECT
                ROW_NUMBER() OVER (ORDER BY AVG(jl.[Total Duration (hrs)]) DESC) AS sort_order
            FROM [Job Lifecyle] AS jl
            """,
            """
            SELECT ROW_NUMBER() OVER (ORDER BY AVG(jl.VH_TOT) DESC) AS sort_order
            FROM landscapeQuery_strategy_A.VH AS jl
            """);
}
