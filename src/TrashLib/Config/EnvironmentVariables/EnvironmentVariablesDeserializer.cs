using Common;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;


namespace TrashLib.Config.EnvironmentVariables;

public record EnvironmentVariableTag;

public class EnvironmentVariablesDeserializer : INodeDeserializer
{
    private readonly IEnvironment _environment;

    public EnvironmentVariablesDeserializer(IEnvironment environment)
    {
        _environment = environment;
    }

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value)
    {
        // Only process items flagged as Environment Variables
        if (expectedType != typeof(EnvironmentVariableTag))
        {
            value = null;
            return false;
        }

        var environmentVariableScalar = reader.Consume<Scalar>();

        //TODO: Add Defaults
        var environmentVariableKey = environmentVariableScalar.Value;

        var environmentVariableValue = _environment.GetEnvironmentVariable(environmentVariableKey);

        if (environmentVariableValue == null) {
            throw new EnvironmentVariableNotDefinedException(environmentVariableScalar.Start.Line, environmentVariableKey);
        }
        // if (!_secrets.Secrets.TryGetValue(secretKey.Value, out var secretsValue))
        // {
        //     throw new SecretNotFoundException(secretKey.Start.Line, secretKey.Value);
        // }

        // value = secretsValue;
        value = environmentVariableValue;
        return true;
    }
}
