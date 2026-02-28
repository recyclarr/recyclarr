using Recyclarr.Yaml;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;

namespace Recyclarr.Settings;

[ForYamlFileTypes(YamlFileType.Settings)]
public class SettingsDeprecatedPropertyBehavior : IYamlBehavior
{
    private static readonly Dictionary<string, RemovedPropertyEntry> RemovedProperties = new(
        StringComparer.Ordinal
    )
    {
        ["repositories"] = new RemovedPropertyEntry(
            "The `repositories` setting has been removed. Use `resource_providers` instead. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers",
            RemovedPropertySeverity.Error
        ),
        ["repository"] = new RemovedPropertyEntry(
            "The `repository` setting has been removed. Use `resource_providers` instead. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers",
            RemovedPropertySeverity.Error
        ),
    };

    public IReadOnlyList<string> Deprecations => _deprecations;
    private readonly List<string> _deprecations = [];

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeInspector(
            inner => new DeprecatedPropertyInspector(inner, RemovedProperties, _deprecations.Add),
            syntax => syntax.OnTop()
        );
    }
}
