using Recyclarr.Yaml;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

[ForYamlFileTypes(YamlFileType.Config)]
internal class ConfigDeprecatedPropertyBehavior(IConfigDiagnosticCollector collector)
    : IYamlBehavior
{
    private static readonly Dictionary<string, RemovedPropertyEntry> RemovedProperties = new(
        StringComparer.Ordinal
    )
    {
        ["replace_existing_custom_formats"] = new RemovedPropertyEntry(
            "The `replace_existing_custom_formats` option has been removed and will be ignored. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#replace-existing-removed",
            RemovedPropertySeverity.Warning
        ),
        ["quality_profiles"] = new RemovedPropertyEntry(
            "The `quality_profiles` element under `custom_formats` has been renamed to "
                + "`assign_scores_to`. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#assign-scores-to",
            RemovedPropertySeverity.Error
        ),
    };

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeInspector(
            inner => new DeprecatedPropertyInspector(
                inner,
                RemovedProperties,
                collector.AddDeprecation
            ),
            syntax => syntax.OnTop()
        );
    }
}
