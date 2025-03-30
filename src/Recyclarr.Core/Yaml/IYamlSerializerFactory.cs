using YamlDotNet.Serialization;

namespace Recyclarr.Yaml;

public interface IYamlSerializerFactory
{
    IDeserializer CreateDeserializer(YamlFileType yamlType);
}
