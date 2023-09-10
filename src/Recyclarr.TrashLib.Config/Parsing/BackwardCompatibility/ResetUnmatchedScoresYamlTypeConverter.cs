using System.ComponentModel;
using System.Globalization;

namespace Recyclarr.TrashLib.Config.Parsing.BackwardCompatibility;

public class ResetUnmatchedScoresYamlTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(bool) || sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        var enabledFlag = Convert.ToBoolean(value);
        return new ResetUnmatchedScoresConfigYaml
        {
            FromBool = true,
            Enabled = enabledFlag
        };
    }
}
