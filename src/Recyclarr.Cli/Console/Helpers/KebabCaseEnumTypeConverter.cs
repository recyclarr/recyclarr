using System.ComponentModel;
using System.Globalization;

namespace Recyclarr.Cli.Console.Helpers;

internal sealed class KebabCaseEnumTypeConverter<TEnum> : TypeConverter
    where TEnum : struct, Enum
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value
    )
    {
        if (value is not string stringValue)
        {
            return base.ConvertFrom(context, culture, value);
        }

        // Convert kebab-case to PascalCase: "custom-formats" -> "CustomFormats"
        var normalized = string.Concat(
            stringValue
                .Split('-')
                .Select(part =>
                    part.Length > 0
                        ? char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()
                        : string.Empty
                )
        );

        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"'{stringValue}' is not a valid value for {typeof(TEnum).Name}. "
                + $"Valid values: {string.Join(", ", GetKebabCaseValues())}"
        );
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType
    )
    {
        if (destinationType == typeof(string) && value is TEnum enumValue)
        {
            return StringCaseConverter.ToKebabCase(enumValue.ToString());
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    private static IEnumerable<string> GetKebabCaseValues()
    {
        return Enum.GetNames<TEnum>().Select(StringCaseConverter.ToKebabCase);
    }
}
