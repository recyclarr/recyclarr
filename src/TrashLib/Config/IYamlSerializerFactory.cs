using YamlDotNet.Serialization;

namespace TrashLib.Config;

public interface IYamlSerializerFactory
{
    IDeserializer CreateDeserializer();
    ISerializer CreateSerializer();
}
