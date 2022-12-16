using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Yaml;

public interface IYamlBehavior
{
    void Setup(DeserializerBuilder builder);
}
