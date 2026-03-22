namespace Recyclarr.Cli.Console.Wizard;

// Filters profiles and CF groups to a specific content category.
// Maps to the group names in the TRaSH Guides groups.json files.
internal enum GuideCategory
{
    Standard,
    Anime,
    French,
    German,
}

internal enum MediaServer
{
    None,
    Plex,
    Emby,
    Jellyfin,
}

internal enum NamingIdType
{
    Imdb,
    Tvdb,
    Tmdb,
}

internal static class NamingIdTypeExtensions
{
    extension(NamingIdType idType)
    {
        internal string DisplayName =>
            idType switch
            {
                NamingIdType.Imdb => "IMDb",
                NamingIdType.Tvdb => "TVDb",
                NamingIdType.Tmdb => "TMDb",
                _ => idType.ToString(),
            };
    }
}

// A user selection identified by trash_id with a display-friendly label
internal record WizardSelection(string TrashId, string Label);

internal static class FlagSelectorHelper
{
    // Decode a FlagSelector bitmask into a list of items using a selector
    // function that maps each selected index to the desired output.
    public static IReadOnlyList<T> DecodeFlagValue<T>(
        int? flagValue,
        int count,
        Func<int, T> selector
    )
    {
        if (flagValue is null or 0)
        {
            return [];
        }

        return Enumerable
            .Range(0, count)
            .Where(i => (flagValue.Value & (1 << i)) != 0)
            .Select(selector)
            .ToList();
    }
}
