using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Secrets;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public record SecretTag;

public class SecretsDeserializer : INodeDeserializer
{
    private readonly ISecretsProvider _secrets;

    public SecretsDeserializer(ISecretsProvider secrets)
    {
        _secrets = secrets;
    }

    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value)
    {
        // Only process items flagged as Secrets
        if (expectedType != typeof(SecretTag))
        {
            value = null;
            return false;
        }

        var secretKey = reader.Consume<Scalar>();
        if (!_secrets.Secrets.TryGetValue(secretKey.Value, out var secretsValue))
        {
            throw new SecretNotFoundException(secretKey.Start.Line, secretKey.Value);
        }

        value = secretsValue;
        return true;
    }
}
