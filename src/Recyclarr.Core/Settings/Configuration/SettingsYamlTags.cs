using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Recyclarr.Config.EnvironmentVariables;
using Recyclarr.Config.File;
using Recyclarr.Config.Secrets;

namespace Recyclarr.Settings.Configuration;

/// <summary>
/// Resolves the value tags <c>settings.yml</c> shares with configuration files (<c>!env_var</c>,
/// <c>!secret</c>, <c>!file</c>) with the same rules.
/// </summary>
internal sealed class SettingsYamlTags(IConfiguration environment, IDirectoryInfo configDirectory)
{
    private readonly Lazy<Dictionary<string, string>> _secrets = new(() =>
        SecretsProvider.Load(configDirectory)
    );

    /// <summary>
    /// The resolved value, or the value itself for any other tag. Throws when a tag cannot be
    /// resolved; the message says why.
    /// </summary>
    public string Resolve(string tag, string value, long line)
    {
        return tag switch
        {
            "!env_var" => EnvironmentVariablesDeserializer.Resolve(environment, value, line),
            "!secret" => _secrets.Value.TryGetValue(value, out var secret)
                ? secret
                : throw new SecretNotFoundException(line, value),
            "!file" => FileDeserializer.Read(configDirectory.FileSystem, configDirectory, value),
            _ => value,
        };
    }
}
