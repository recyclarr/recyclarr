using System;
using System.Globalization;

namespace Trash.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string value, string searchFor)
        {
            return value.Contains(searchFor, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this string value, string matchThis)
        {
            return value.Equals(matchThis, StringComparison.OrdinalIgnoreCase);
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
    }
}
