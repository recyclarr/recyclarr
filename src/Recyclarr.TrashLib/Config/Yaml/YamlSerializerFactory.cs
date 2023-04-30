using Recyclarr.Common.YamlDotNet;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
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
        var builder = new DeserializerBuilder();

        // This MUST be first (amongst the other node type resolvers) because that means it will be processed LAST. This
        // is a last resort utility resolver to make error messages more clear. We do not want it interfering with other
        // resolvers.
        builder.WithNodeTypeResolver(new SyntaxErrorHelper());

        builder
            .IgnoreFields()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithNodeDeserializer(new ForceEmptySequences(_objectFactory))
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithObjectFactory(_objectFactory);

        foreach (var behavior in _behaviors)
        {
            behavior.Setup(builder);
        }

        return builder.Build();
    }
}
