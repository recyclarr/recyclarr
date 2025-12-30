using System.ComponentModel;

namespace Recyclarr.Cli.Console.Helpers;

[TypeConverter(typeof(KebabCaseEnumTypeConverter<CacheableResourceType>))]
internal enum CacheableResourceType
{
    CustomFormats,
    QualityProfiles,
}
