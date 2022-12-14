using Common;
using Common.YamlDotNet;
using TrashLib.Config.Secrets;
using TrashLib.Config.EnvironmentVariables;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TrashLib.Config;

public class YamlSerializerFactory : IYamlSerializerFactory
{
    private readonly IObjectFactory _objectFactory;
    private readonly ISecretsProvider _secretsProvider;
    private readonly IEnvironment _environment;

    public YamlSerializerFactory(IObjectFactory objectFactory, ISecretsProvider secretsProvider, IEnvironment environment)
    {
        _objectFactory = objectFactory;
        _secretsProvider = secretsProvider;
        _environment = environment;
    }

    public IDeserializer CreateDeserializer(Action<DeserializerBuilder>? extraBuilder = null)
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlNullableEnumTypeConverter())
            .WithNodeDeserializer(new SecretsDeserializer(_secretsProvider))
            .WithNodeDeserializer(new EnvironmentVariablesDeserializer(_environment))
            .WithTagMapping("!secret", typeof(SecretTag))
            .WithTagMapping("!env_var", typeof(EnvironmentVariableTag))
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
