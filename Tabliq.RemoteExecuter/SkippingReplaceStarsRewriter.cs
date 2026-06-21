using Tabliq.Sql.Binding;
using Tabliq.Sql.Rewriter;

namespace Tabliq.RemoteExecuter;

internal class SkippingReplaceStarsRewriter : ReplaceStarsRewriter
{
    public static readonly SkippingReplaceStarsRewriter Instance = new SkippingReplaceStarsRewriter();
    protected override bool ShouldExpand(ColumnBinding binding)
    {
        if (binding.ColumnSymbol.State is VirtualColumn virtualColumn && virtualColumn.ExcludeFromExpansion)
        {
            return false;
        }

        return base.ShouldExpand(binding);
    }
}
