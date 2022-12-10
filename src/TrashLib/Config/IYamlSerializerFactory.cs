using YamlDotNet.Serialization;

namespace TrashLib.Config;

public interface IYamlSerializerFactory
{
    IDeserializer CreateDeserializer(Action<DeserializerBuilder>? extraBuilder = null);
    ISerializer CreateSerializer();
}
