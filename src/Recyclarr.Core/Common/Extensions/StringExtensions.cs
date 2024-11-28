using System.Globalization;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace Recyclarr.Common.Extensions;

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
        return string.Format(CultureInfo.InvariantCulture, value, args);
    }

    public static string TrimNewlines(this string value)
    {
        return value.Trim('\r', '\n');
    }

    public static string ToCamelCase(this string value)
    {
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    public static string ToSnakeCase(this string text)
    {
        if (text.Length < 2)
        {
            return text;
        }

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        foreach (var c in text[1..])
        {
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
