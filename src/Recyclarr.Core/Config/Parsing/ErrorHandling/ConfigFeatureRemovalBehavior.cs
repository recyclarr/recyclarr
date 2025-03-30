using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

[ForYamlFileTypes(YamlFileType.Config)]
public class ConfigFeatureRemovalBehavior : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder.WithNodeTypeResolver(new FeatureRemovalChecker());
    }
}
