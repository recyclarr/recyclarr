using Recyclarr.Yaml;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

[ForYamlFileTypes(YamlFileType.Config)]
internal class ConfigDeprecatedPropertyBehavior(IConfigDiagnosticCollector collector)
    : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeInspector(
            inner => new DeprecatedPropertyInspector(inner, collector),
            syntax => syntax.OnTop()
        );
    }
}
