using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Secrets;

[UsedImplicitly]
public class SecretsYamlBehavior(ISecretsProvider secretsProvider) : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new SecretsDeserializer(secretsProvider))
            .WithTagMapping("!secret", typeof(SecretTag));
    }
}
