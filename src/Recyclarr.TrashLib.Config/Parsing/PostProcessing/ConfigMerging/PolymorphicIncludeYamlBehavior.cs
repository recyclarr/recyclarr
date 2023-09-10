using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

[UsedImplicitly]
public class PolymorphicIncludeYamlBehavior : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeDiscriminatingNodeDeserializer(o => o
            .AddUniqueKeyTypeDiscriminator<IYamlInclude>(new Dictionary<string, Type>
            {
                [nameof(ConfigYamlInclude.Config).ToSnakeCase()] = typeof(ConfigYamlInclude),
                [nameof(TemplateYamlInclude.Template).ToSnakeCase()] = typeof(TemplateYamlInclude)
            }));
    }
}
