using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Secrets;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Yaml;

[UsedImplicitly]
public class SecretsYamlBehavior : IYamlBehavior
{
    private readonly ISecretsProvider _secretsProvider;

    public SecretsYamlBehavior(ISecretsProvider secretsProvider)
    {
        _secretsProvider = secretsProvider;
    }

    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new SecretsDeserializer(_secretsProvider))
            .WithTagMapping("!secret", typeof(SecretTag));
    }
}
