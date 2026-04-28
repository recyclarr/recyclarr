using Recyclarr.Common;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

public class DataSizeYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(DataSize);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        try
        {
            return DataSize.Parse(scalar.Value);
        }
        catch (FormatException ex)
        {
            throw new YamlException(scalar.Start, scalar.End, ex.Message, ex);
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotSupportedException("DataSize serialization is not supported");
    }
}
