using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal record InvalidNamingEntry(string Type, string ConfigValue);

internal class NamingFormatLookup
{
    private readonly List<InvalidNamingEntry> _errors = [];
    public IReadOnlyCollection<InvalidNamingEntry> Errors => _errors;

    public string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey,
        string errorDescription
    )
    {
        return ObtainFormat(guideFormats, configFormatKey, keySuffix: null, errorDescription);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    public string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey,
        string? keySuffix,
        string errorDescription
    )
    {
        if (configFormatKey is null)
        {
            return null;
        }

        // Use lower-case for the config value because System.Text.Json doesn't let us create a
        // case-insensitive dictionary. The MediaNamingGuideService converts all parsed guide JSON
        // keys to lower case.
        var lowerKey = configFormatKey.ToLowerInvariant();

        var keys = new List<string> { lowerKey };
        if (keySuffix is not null)
        {
            // Put the more specific key first
            keys.Insert(index: 0, lowerKey + keySuffix);
        }

        foreach (var k in keys)
        {
            if (guideFormats.TryGetValue(k, out var format))
            {
                return format;
            }
        }

        _errors.Add(new InvalidNamingEntry(errorDescription, configFormatKey));
        return null;
    }
}
