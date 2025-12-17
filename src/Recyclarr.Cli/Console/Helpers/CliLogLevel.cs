using System.ComponentModel;

namespace Recyclarr.Cli.Console.Helpers;

[TypeConverter(typeof(KebabCaseEnumTypeConverter<CliLogLevel>))]
internal enum CliLogLevel
{
    Debug,
    Info,
    Warn,
}
