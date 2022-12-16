using Recyclarr.Common.YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.TrashLib.Config.Yaml;

public class YamlSerializerFactory : IYamlSerializerFactory
{
    private readonly IObjectFactory _objectFactory;
    private readonly IEnumerable<IYamlBehavior> _behaviors;

    public YamlSerializerFactory(IObjectFactory objectFactory, IEnumerable<IYamlBehavior> behaviors)
    {
        _objectFactory = objectFactory;
        _behaviors = behaviors;
    }

    public IDeserializer CreateDeserializer()
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithNodeDeserializer(new ForceEmptySequences(_objectFactory))
            .WithObjectFactory(_objectFactory);

        foreach (var behavior in _behaviors)
        {
            behavior.Setup(builder);
        }

        return builder.Build();
    }
}
