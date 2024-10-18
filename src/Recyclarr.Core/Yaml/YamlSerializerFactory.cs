using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Yaml;

public class YamlSerializerFactory(IObjectFactory objectFactory, IReadOnlyCollection<IYamlBehavior> behaviors)
    : IYamlSerializerFactory
{
    public IDeserializer CreateDeserializer()
    {
        var builder = new DeserializerBuilder();

        // This MUST be first (amongst the other node type resolvers) because that means it will be processed LAST. This
        // is a last resort utility resolver to make error messages more clear. We do not want it interfering with other
        // resolvers.
        builder.WithNodeTypeResolver(new SyntaxErrorHelper());

        CommonSetup(builder);

        builder
            .WithNodeDeserializer(new ForceEmptySequences(objectFactory))
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithObjectFactory(objectFactory)
            .WithDuplicateKeyChecking();

        foreach (var behavior in behaviors)
        {
            behavior.Setup(builder);
        }

        return builder.Build();
    }

    public ISerializer CreateSerializer()
    {
        var builder = new SerializerBuilder();
        CommonSetup(builder);

        builder
            .DisableAliases()
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitEmptyCollections |
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitDefaults);

        return builder.Build();
    }

    private static void CommonSetup<T>(BuilderSkeleton<T> builder)
        where T : BuilderSkeleton<T>
    {
        builder
            .IgnoreFields()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter());
    }
}
