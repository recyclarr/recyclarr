using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Yaml;

public interface IYamlSerializerFactory
{
    IDeserializer CreateDeserializer();
    ISerializer CreateSerializer();
}
