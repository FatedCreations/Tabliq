namespace Tabliq.Execution.ExpressionPlan;

public sealed class LikeConditionExecutionPlan : ConditionExecutionPlan
{
    public LikeConditionExecutionPlan(ExpressionPlanNode left, ExpressionPlanNode right, bool isNot)
    {
        Left = left;
        Right = right;
        IsNot = isNot;
    }

    public ExpressionPlanNode Left { get; }
    public ExpressionPlanNode Right { get; }
    public bool IsNot { get; }

    public override bool Execute(RowAccessor row)
    {
        var left = Left.ExecuteExpression(row)?.ToString();
        var right = Right.ExecuteExpression(row)?.ToString();
        var result = MatchesLike(left, right);
        return IsNot ? !result : result;
    }
    private static bool MatchesLike(string? left, string? pattern)
    {
        if (left is null || pattern is null)
        {
            return left is null && pattern is null;
        }

        var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("%", ".*")
            .Replace("_", ".");

        return System.Text.RegularExpressions.Regex.IsMatch(left, $"^{regexPattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
    public override IEnumerable<ExpressionPlanNode> GetExpressions() => [Left, Right];
}
