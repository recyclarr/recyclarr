using Autofac.Features.Indexed;
using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Yaml;

public class YamlSerializerFactory(
    IObjectFactory objectFactory,
    IIndex<YamlFileType, IReadOnlyCollection<IYamlBehavior>> behaviorIndex
) : IYamlSerializerFactory
{
    public IDeserializer CreateDeserializer(YamlFileType yamlType)
    {
        var builder = new DeserializerBuilder();

        // This MUST be first (amongst the other node type resolvers) because that means it will be
        // processed LAST. This is a last resort utility resolver to make error messages more clear.
        // We do not want it interfering with other resolvers.
        builder.WithNodeTypeResolver(new SyntaxErrorHelper());

        builder
            .IgnoreFields()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithNodeDeserializer(new ForceEmptySequences(objectFactory))
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithObjectFactory(objectFactory)
            .WithDuplicateKeyChecking();

        unkeyed registrations are not being included. run unit tests to see failures.
        var behaviors = behaviorIndex[YamlFileType.Config];
        foreach (var behavior in behaviors)
        {
            behavior.Setup(builder);
        }

        return builder.Build();
    }
}
