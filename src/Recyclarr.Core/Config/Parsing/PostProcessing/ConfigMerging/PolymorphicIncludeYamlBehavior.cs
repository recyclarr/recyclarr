using System.Text.Json;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

[ForYamlFileTypes(YamlFileType.Config)]
public class PolymorphicIncludeYamlBehavior : IYamlBehavior
{
    private static readonly Dictionary<string, Type> Mapping = new()
    {
        [JsonNamingPolicy.SnakeCaseLower.ConvertName(nameof(ConfigYamlInclude.Config))] =
            typeof(ConfigYamlInclude),
        [JsonNamingPolicy.SnakeCaseLower.ConvertName(nameof(TemplateYamlInclude.Template))] =
            typeof(TemplateYamlInclude),
    };

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeDiscriminatingNodeDeserializer(o =>
            o.AddUniqueKeyTypeDiscriminator<IYamlInclude>(Mapping)
        );
    }
}
