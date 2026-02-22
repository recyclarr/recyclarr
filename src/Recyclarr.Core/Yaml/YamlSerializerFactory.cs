using Recyclarr.Yaml.YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Yaml;

internal class YamlSerializerFactory(
    IObjectFactory objectFactory,
    YamlBehaviorProvider behaviorProvider
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
            .WithNodeDeserializer(new ScalarMismatchDeserializer())
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithObjectFactory(objectFactory)
            .WithDuplicateKeyChecking();

        foreach (var behavior in behaviorProvider.GetBehaviors(yamlType))
        {
            behavior.Setup(builder);
        }

        return builder.Build();
    }
}
