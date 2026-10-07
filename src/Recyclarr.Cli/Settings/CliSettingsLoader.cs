using System.IO.Abstractions;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Recyclarr.Cli.Settings;

/// <summary>
/// Reads cli.yml from the configuration directory the server also uses. Only the CLI reads this
/// file, so parsing is strict: a key the CLI does not know is an error, not a silent no-op.
/// </summary>
/// <remarks>
/// The node tree is walked by hand rather than deserialized, so every error names the YAML path;
/// YamlDotNet's deserializer names C# types instead. A non-empty <c>RECYCLARR_SERVER_URL</c>
/// overrides <c>server.base_url</c>, so an image can target its own server without a file in the
/// user's volume.
/// </remarks>
internal sealed class CliSettingsLoader(ConfigDirectoryLocator configLocator, IEnvironment env)
{
    public const string FileName = "cli.yml";
    public const string ServerUrlVariable = "RECYCLARR_SERVER_URL";

    public CliSettings Load()
    {
        var settings = LoadFile();

        var envUrl = env.GetEnvironmentVariable(ServerUrlVariable);
        if (string.IsNullOrEmpty(envUrl))
        {
            return settings;
        }

        return settings with
        {
            ServerBaseUrl = ParseBaseUrl(envUrl, ServerUrlVariable),
        };
    }

    private CliSettings LoadFile()
    {
        var file = configLocator.Locate().File(FileName);
        if (!file.Exists)
        {
            return new CliSettings();
        }

        try
        {
            var root = Parse(file.OpenText);
            var server = Mapping(root, "server", ["base_url"]);
            var baseUrl = Scalar(server, "base_url", "server.base_url");
            return new CliSettings
            {
                ServerBaseUrl = string.IsNullOrEmpty(baseUrl)
                    ? null
                    : ParseBaseUrl(baseUrl, "'server.base_url'"),
            };
        }
        catch (CliSettingsException e)
        {
            throw new CliSettingsException($"{file.FullName}: {e.Message}");
        }
    }

    private static YamlMappingNode? Parse(Func<TextReader> open)
    {
        var stream = new YamlStream();
        try
        {
            using var reader = open();
            stream.Load(reader);
        }
        catch (YamlException e)
        {
            throw new CliSettingsException($"invalid YAML at line {e.Start.Line}");
        }

        return stream.Documents.FirstOrDefault()?.RootNode switch
        {
            null or YamlScalarNode { Value: null or "" } => null,
            YamlMappingNode mapping => Validate(mapping, "", ["server"]),
            _ => throw new CliSettingsException("the file must contain a mapping"),
        };
    }

    private static YamlMappingNode? Mapping(
        YamlMappingNode? parent,
        string key,
        IReadOnlyCollection<string> allowedKeys
    )
    {
        return Child(parent, key) switch
        {
            null or YamlScalarNode { Value: null or "" } => null,
            YamlMappingNode mapping => Validate(mapping, $"{key}.", allowedKeys),
            _ => throw new CliSettingsException($"'{key}' must be a mapping"),
        };
    }

    private static string? Scalar(YamlMappingNode? parent, string key, string path)
    {
        return Child(parent, key) switch
        {
            null => null,
            YamlScalarNode scalar => scalar.Value,
            _ => throw new CliSettingsException($"'{path}' must be a single value"),
        };
    }

    private static YamlNode? Child(YamlMappingNode? parent, string key)
    {
        return parent?.Children.TryGetValue(new YamlScalarNode(key), out var node) == true
            ? node
            : null;
    }

    private static YamlMappingNode Validate(
        YamlMappingNode mapping,
        string prefix,
        IReadOnlyCollection<string> allowedKeys
    )
    {
        var unknown = mapping
            .Children.Keys.Select(k => (k as YamlScalarNode)?.Value ?? k.ToString())
            .FirstOrDefault(k => !allowedKeys.Contains(k));

        if (unknown is not null)
        {
            throw new CliSettingsException($"unknown setting '{prefix}{unknown}'");
        }

        return mapping;
    }

    private static Uri ParseBaseUrl(string value, string source)
    {
        if (
            Uri.TryCreate(value, UriKind.Absolute, out var url)
            && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        )
        {
            return url;
        }

        throw new CliSettingsException(
            $"{source} must be an absolute http or https URL, "
                + $"for example http://recyclarr:7982 (got '{value}')"
        );
    }
}
