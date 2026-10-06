using Tabliq.Execution.Functions;
using Tabliq.Sql.Binding;
using Tabliq.Sql.Rewriter;

namespace Tabliq.Execution.SqlServer.Internal;

internal class SkippingReplaceStarsRewriter : ReplaceStarsRewriter
{
    public static readonly SkippingReplaceStarsRewriter Instance = new SkippingReplaceStarsRewriter();
    protected override bool ShouldExpand(ColumnBinding binding)
    {
        if (binding.ColumnSymbol.GetState<VirtualColumn>() is VirtualColumn virtualColumn && virtualColumn.ExcludeFromExpansion)
        {
            return false;
        }

        return base.ShouldExpand(binding);
    }
}
