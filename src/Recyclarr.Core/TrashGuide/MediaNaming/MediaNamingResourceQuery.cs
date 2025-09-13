using System.Diagnostics.CodeAnalysis;
using Recyclarr.Json.Loading;

namespace Recyclarr.TrashGuide.MediaNaming;

public class MediaNamingResourceQuery(
    IEnumerable<IMediaNamingResourceProvider> providers,
    GuideJsonLoader jsonLoader
) : IMediaNamingResourceQuery
{
    private RadarrMediaNamingData? _radarrCache;
    private SonarrMediaNamingData? _sonarrCache;

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    private static Dictionary<string, string> JoinDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dictionaries
    )
    {
        return dictionaries
            .SelectMany(dict => dict)
            .DistinctBy(kvp => kvp.Key.ToLowerInvariant()) // first occurrence wins
            .ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
    }

    public RadarrMediaNamingData GetRadarrNamingData()
    {
        if (_radarrCache is not null)
            return _radarrCache;

        // Get media naming directories from all providers
        var paths = providers.SelectMany(provider =>
            provider.GetMediaNamingPaths(SupportedServices.Radarr)
        );

        var data = jsonLoader.LoadAllFilesAtPaths<RadarrMediaNamingData>(paths);
        _radarrCache = new RadarrMediaNamingData
        {
            File = JoinDictionaries(data.Select(x => x.File)),
            Folder = JoinDictionaries(data.Select(x => x.Folder)),
        };
        return _radarrCache;
    }

    public SonarrMediaNamingData GetSonarrNamingData()
    {
        if (_sonarrCache is not null)
            return _sonarrCache;

        // Get media naming directories from all providers
        var paths = providers.SelectMany(provider =>
            provider.GetMediaNamingPaths(SupportedServices.Sonarr)
        );

        var data = jsonLoader.LoadAllFilesAtPaths<SonarrMediaNamingData>(paths);
        _sonarrCache = new SonarrMediaNamingData
        {
            Season = JoinDictionaries(data.Select(x => x.Season)),
            Series = JoinDictionaries(data.Select(x => x.Series)),
            Episodes = new SonarrEpisodeNamingData
            {
                Anime = JoinDictionaries(data.Select(x => x.Episodes.Anime)),
                Daily = JoinDictionaries(data.Select(x => x.Episodes.Daily)),
                Standard = JoinDictionaries(data.Select(x => x.Episodes.Standard)),
            },
        };
        return _sonarrCache;
    }
}
