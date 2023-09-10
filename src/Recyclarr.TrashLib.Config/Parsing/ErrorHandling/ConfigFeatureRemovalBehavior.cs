using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing.ErrorHandling;

public class ConfigFeatureRemovalBehavior : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder.WithNodeTypeResolver(new FeatureRemovalChecker());
    }
}
