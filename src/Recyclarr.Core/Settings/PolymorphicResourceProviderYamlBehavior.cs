using Recyclarr.Settings.Models;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Settings;

[UsedImplicitly]
[ForYamlFileTypes(YamlFileType.Settings)]
public class PolymorphicResourceProviderYamlBehavior : IYamlBehavior
{
    private static readonly Dictionary<string, Type> Mapping = new()
    {
        ["clone_url"] = typeof(GitResourceProvider),
        ["path"] = typeof(LocalResourceProvider),
    };

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeDiscriminatingNodeDeserializer(o =>
            o.AddUniqueKeyTypeDiscriminator<ResourceProvider>(Mapping)
        );
    }
}
