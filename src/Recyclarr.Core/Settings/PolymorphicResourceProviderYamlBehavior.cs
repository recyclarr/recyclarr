using Recyclarr.Common.Extensions;
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
        [nameof(GitRepositorySource.CloneUrl).ToSnakeCase()] = typeof(GitRepositorySource),
        [nameof(LocalPathSource.Path).ToSnakeCase()] = typeof(LocalPathSource),
    };

    public void Setup(DeserializerBuilder builder)
    {
        builder.WithTypeDiscriminatingNodeDeserializer(o =>
            o.AddUniqueKeyTypeDiscriminator<IUnderlyingResourceProvider>(Mapping)
        );
    }
}
