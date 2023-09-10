using YamlDotNet.Serialization;

namespace Recyclarr.Yaml;

public interface IYamlBehavior
{
    void Setup(DeserializerBuilder builder);
}
