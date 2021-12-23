using Common.YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TrashLib.Config;

public class YamlSerializerFactory : IYamlSerializerFactory
{
    private readonly IObjectFactory _objectFactory;

    public YamlSerializerFactory(IObjectFactory objectFactory)
    {
        _objectFactory = objectFactory;
    }

    public IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithObjectFactory(_objectFactory)
            .Build();
    }

    public ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .Build();
    }
}
