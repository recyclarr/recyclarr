using System.ComponentModel;

namespace Recyclarr.Cli.Console.Helpers;

[TypeConverter(typeof(KebabCaseEnumTypeConverter<StatefulResourceType>))]
internal enum StatefulResourceType
{
    CustomFormats,
    QualityProfiles,
}
