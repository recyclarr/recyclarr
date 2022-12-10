using Common.YamlDotNet;
using TrashLib.Config.Secrets;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TrashLib.Config;

public class YamlSerializerFactory : IYamlSerializerFactory
{
    private readonly IObjectFactory _objectFactory;
    private readonly ISecretsProvider _secretsProvider;

    public YamlSerializerFactory(IObjectFactory objectFactory, ISecretsProvider secretsProvider)
    {
        _objectFactory = objectFactory;
        _secretsProvider = secretsProvider;
    }

    public IDeserializer CreateDeserializer(Action<DeserializerBuilder>? extraBuilder = null)
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithNodeDeserializer(new SecretsDeserializer(_secretsProvider))
            .WithTagMapping("!secret", typeof(SecretTag))
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .WithNodeDeserializer(new ForceEmptySequences(_objectFactory))
            .WithObjectFactory(_objectFactory);

        extraBuilder?.Invoke(builder);

        return builder.Build();
    }

    public ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .Build();
    }
}
