using Recyclarr.Common.Extensions;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

[UsedImplicitly]
[ForYamlFileTypes(YamlFileType.Config)]
public class PolymorphicIncludeYamlBehavior : IYamlBehavior
{
    private static readonly Dictionary<string, Type> Mapping = new()
    {
        [nameof(ConfigYamlInclude.Config).ToSnakeCase()] = typeof(ConfigYamlInclude),
        [nameof(TemplateYamlInclude.Template).ToSnakeCase()] = typeof(TemplateYamlInclude),
    };

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeDiscriminatingNodeDeserializer(o =>
            o.AddUniqueKeyTypeDiscriminator<IYamlInclude>(Mapping)
        );
    }
}
