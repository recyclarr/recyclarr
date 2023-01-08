using System.ComponentModel;
using System.Text;

namespace Recyclarr.Cli.Console.Helpers;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EnumDescriptionAttribute<TEnum> : DescriptionAttribute
    where TEnum : Enum
{
    public override string Description { get; }

    public EnumDescriptionAttribute(string description)
    {
        var enumNames = Enum.GetNames(typeof(TEnum))
            .Select(x => x.ToLowerInvariant());

        var str = new StringBuilder(description.Trim());
        str.Append($" (Valid Values: {string.Join(", ", enumNames)})");
        Description = str.ToString();
    }
}
