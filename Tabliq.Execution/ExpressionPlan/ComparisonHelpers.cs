namespace Tabliq.Execution.ExpressionPlan;

public static class ComparisonHelpers
{
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            return Convert.ToDecimal(left, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDecimal(right, System.Globalization.CultureInfo.InvariantCulture);
        }

        return left.Equals(right);
    }

    public static int Compare(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftDecimal = Convert.ToDecimal(left, System.Globalization.CultureInfo.InvariantCulture);
            var rightDecimal = Convert.ToDecimal(right, System.Globalization.CultureInfo.InvariantCulture);
            return leftDecimal.CompareTo(rightDecimal);
        }

        return Comparer<object?>.Default.Compare(left, right);
    }

    public static bool IsNumeric(object value)
        => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}