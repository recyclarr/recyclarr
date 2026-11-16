using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.EnvironmentVariables;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public record EnvironmentVariableTag;

/// <summary>
/// Resolves <c>!env_var NAME [default]</c> tags from environment configuration (see
/// <c>EnvironmentConfiguration</c>), where <c>__</c> in a variable name is a key delimiter.
/// </summary>
public class EnvironmentVariablesDeserializer(IConfiguration configuration) : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        // Only process items flagged as Environment Variables
        if (expectedType != typeof(EnvironmentVariableTag))
        {
            value = null;
            return false;
        }

        var scalar = reader.Consume<Scalar>();
        value = Resolve(configuration, scalar.Value, scalar.Start.Line);
        return true;
    }

    /// <summary>
    /// The value of a <c>!env_var</c> tag: the variable, or the default after its name when the
    /// variable is unset or blank.
    /// </summary>
    public static string Resolve(IConfiguration configuration, string tagValue, long line)
    {
        var split = tagValue.Trim().Split(' ', 2);
        var key = split[0].Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        var envVarValue = configuration[key];
        if (string.IsNullOrWhiteSpace(envVarValue))
        {
            // Trim whitespace + quotation characters
            envVarValue = split.ElementAtOrDefault(1)?.Trim().Trim('\'', '"');
        }

        return envVarValue ?? throw new EnvironmentVariableNotDefinedException(line, tagValue);
    }
}
