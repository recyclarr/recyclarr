using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Recyclarr.Cli.Console.Helpers;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class EnumDescriptionAttribute<TEnum> : DescriptionAttribute
    where TEnum : Enum
{
    public override string Description { get; }

    public EnumDescriptionAttribute(string description)
    {
        var enumNames = Enum.GetNames(typeof(TEnum)).Select(StringCaseConverter.ToKebabCase);

        var str = new StringBuilder(description.Trim());
        str.Append(
            CultureInfo.InvariantCulture,
            $" (Valid Values: {string.Join(", ", enumNames)})"
        );
        Description = str.ToString();
    }
}
