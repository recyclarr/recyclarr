using System.ComponentModel;
using System.Globalization;

namespace Recyclarr.Common;

/// <summary>
/// Converts the text form of <see cref="DataSize"/> for the configuration binder.
/// </summary>
internal sealed class DataSizeTypeConverter : TypeConverter
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
        return value is string text
            ? DataSize.Parse(text)
            : base.ConvertFrom(context, culture, value);
    }
}
