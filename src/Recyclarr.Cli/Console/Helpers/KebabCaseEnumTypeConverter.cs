using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Recyclarr.Cli.Console.Helpers;

internal sealed class KebabCaseEnumTypeConverter<TEnum> : TypeConverter
    where TEnum : struct, Enum
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
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

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            if (ToKebabCase(enumValue).Equals(stringValue, StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        throw new InvalidOperationException(
            $"'{stringValue}' is not a valid value for {typeof(TEnum).Name}. "
                + $"Valid values: {string.Join(", ", KebabCaseNames())}"
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
            return ToKebabCase(enumValue);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    public static IEnumerable<string> KebabCaseNames()
    {
        return Enum.GetValues<TEnum>().Select(ToKebabCase);
    }

    private static string ToKebabCase(TEnum value)
    {
        return JsonNamingPolicy.KebabCaseLower.ConvertName(value.ToString());
    }
}
