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
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
        {
            return new Unlimited();
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
        {
            return new Numeric(num);
        }

        throw new FormatException(
            $"Invalid quality size value: '{value}'. Expected a number or 'unlimited'."
        );
    }
}
