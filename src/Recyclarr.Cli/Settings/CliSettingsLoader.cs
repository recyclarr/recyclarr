using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Cli.Settings;

// Reads cli.yml from the configuration directory the server also uses. Only the CLI reads this
// file, so parsing is strict: a key the CLI does not know is an error, not a silent no-op.
internal sealed partial class CliSettingsLoader(ConfigDirectoryLocator configLocator)
{
    public const string FileName = "cli.yml";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public CliSettings Load()
    {
        var file = configLocator.Locate().File(FileName);
        if (!file.Exists)
        {
            return new CliSettings();
        }

        CliSettingsYaml? yaml;
        try
        {
            using var reader = file.OpenText();
            yaml = Deserializer.Deserialize<CliSettingsYaml?>(reader);
        }
        catch (YamlException e)
        {
            throw new CliSettingsException($"{file.FullName} (line {e.Start.Line}): {Describe(e)}");
        }

        return new CliSettings { ServerBaseUrl = ParseBaseUrl(yaml?.Server?.BaseUrl, file) };
    }

    private static Uri? ParseBaseUrl(string? value, IFileInfo file)
    {
        if (value is null)
        {
            return null;
        }

        if (
            Uri.TryCreate(value, UriKind.Absolute, out var url)
            && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        )
        {
            return url;
        }

        throw new CliSettingsException(
            $"{file.FullName}: server.base_url must be an absolute http or https URL, "
                + $"for example http://recyclarr:7982 (got '{value}')"
        );
    }

    // YamlDotNet names the C# type in unknown-property errors; report the YAML key instead.
    private static string Describe(YamlException e)
    {
        var unknown = UnknownPropertyRegex.Match(e.Message);
        return unknown.Success
            ? $"unknown setting '{unknown.Groups["name"].Value}'"
            : e.InnerException?.Message ?? e.Message;
    }

    [GeneratedRegex("^Property '(?<name>[^']+)' not found on type")]
    private static partial Regex UnknownPropertyRegex { get; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    private sealed record CliSettingsYaml
    {
        public ServerYaml? Server { get; init; }
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    private sealed record ServerYaml
    {
        public string? BaseUrl { get; init; }
    }
}
