using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Yaml;

[AttributeUsage(AttributeTargets.Class)]
[SuppressMessage(
    "Design",
    "CA1019:Define accessors for attribute arguments",
    Justification = "All arguments are available in concatenated form"
)]
internal sealed class ForYamlFileTypesAttribute(
    YamlFileType firstType,
    params YamlFileType[] additionalTypes
) : Attribute
{
    public YamlFileType[] FileTypes { get; } = [firstType, .. additionalTypes];
}
