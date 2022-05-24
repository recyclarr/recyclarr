using System.Globalization;

namespace Common.Extensions;

public static class StringExtensions
{
    public static bool ContainsIgnoreCase(this string? value, string searchFor)
    {
        return value?.Contains(searchFor, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static bool EqualsIgnoreCase(this string? value, string? matchThis)
    {
        return value?.Equals(matchThis, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static bool EndsWithIgnoreCase(this string? value, string matchThis)
    {
        return value?.EndsWith(matchThis, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static bool StartsWithIgnoreCase(this string? value, string matchThis)
    {
        return value?.StartsWith(matchThis, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static float ToFloat(this string value)
    {
        return float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
    }

    public static decimal ToDecimal(this string value)
    {
        return decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
    }

    public static string FormatWith(this string value, params object[] args)
    {
        return string.Format(value, args);
    }

    public static string TrimNewlines(this string value)
    {
        return value.Trim('\r', '\n');
    }
}
