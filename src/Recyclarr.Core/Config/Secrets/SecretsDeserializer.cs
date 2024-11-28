using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Secrets;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public record SecretTag;

public class SecretsDeserializer(ISecretsProvider secrets) : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        // Only process items flagged as Secrets
        if (expectedType != typeof(SecretTag))
        {
            value = null;
            return false;
        }

        var secretKey = reader.Consume<Scalar>();
        if (!secrets.Secrets.TryGetValue(secretKey.Value, out var secretsValue))
        {
            throw new SecretNotFoundException(secretKey.Start.Line, secretKey.Value);
        }

        value = secretsValue;
        return true;
    }
}
