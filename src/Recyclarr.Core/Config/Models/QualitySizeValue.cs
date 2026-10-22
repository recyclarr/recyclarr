using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Recyclarr.Config.Models;

[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public abstract record QualitySizeValue
{
    public sealed record Numeric(decimal Value) : QualitySizeValue
    {
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record Unlimited : QualitySizeValue
    {
        public override string ToString() => "unlimited";
    }

    public static QualitySizeValue? Parse(string? value)
    {
        return TryParse(value, out var result)
            ? result
            : throw new FormatException(
                $"Invalid quality size value: '{value}'. Expected a number or 'unlimited'."
            );
    }

    /// <summary>Succeeds for a number, <c>unlimited</c>, or no value (a null result).</summary>
    public static bool TryParse(string? value, out QualitySizeValue? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
        {
            result = new Unlimited();
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
        {
            result = new Numeric(num);
            return true;
        }

        return false;
    }
}
