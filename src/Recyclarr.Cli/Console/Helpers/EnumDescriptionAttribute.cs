using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Recyclarr.Cli.Console.Helpers;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class EnumDescriptionAttribute<TEnum> : DescriptionAttribute
    where TEnum : Enum
{
    public override string Description { get; }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    public EnumDescriptionAttribute(string description)
    {
        var enumNames = Enum.GetNames(typeof(TEnum)).Select(x => x.ToLowerInvariant());

        var str = new StringBuilder(description.Trim());
        str.Append(
            CultureInfo.InvariantCulture,
            $" (Valid Values: {string.Join(", ", enumNames)})"
        );
        Description = str.ToString();
    }
}
