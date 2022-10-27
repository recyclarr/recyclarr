using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace TrashLib.Config.Secrets;

public record SecretTag;

public class SecretsDeserializer : INodeDeserializer
{
    private readonly ISecretsProvider _secrets;

    public SecretsDeserializer(ISecretsProvider secrets)
    {
        _secrets = secrets;
    }

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value)
    {
        // Only process items flagged as Secrets
        if (expectedType != typeof(SecretTag))
        {
            value = null;
            return false;
        }

        var scalar = reader.Consume<Scalar>();
        if (!_secrets.Secrets.TryGetValue(scalar.Value, out var secretsValue))
        {
            throw new YamlException(scalar.Start, scalar.End, $"{scalar.Value} is not defined in secrets.yml.");
        }

        value = secretsValue;
        return true;
    }
}
