namespace Recyclarr.Pipelines.MediaNaming;

internal static class NamingFormatLookup
{
    public static string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey
    )
    {
        return ObtainFormat(guideFormats, configFormatKey, keySuffix: null);
    }

    public static string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey,
        string? keySuffix
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

        return null;
    }
}
