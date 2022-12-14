using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config;

public interface IYamlSerializerFactory
{
    IDeserializer CreateDeserializer(Action<DeserializerBuilder>? extraBuilder = null);
    ISerializer CreateSerializer();
}
