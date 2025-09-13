using System.Diagnostics.CodeAnalysis;
using Recyclarr.Json.Loading;

namespace Recyclarr.TrashGuide.MediaNaming;

public class MediaNamingResourceQuery(
    IEnumerable<IMediaNamingResourceProvider> providers,
    GuideJsonLoader jsonLoader
) : IMediaNamingResourceQuery
{
    private readonly Lazy<RadarrMediaNamingData> _radarrCache = new(() =>
    {
        // Get media naming directories from all providers
        var paths = providers.SelectMany(provider =>
            provider.GetMediaNamingPaths(SupportedServices.Radarr)
        );

        var data = jsonLoader.LoadAllFilesAtPaths<RadarrMediaNamingData>(paths);
        return new RadarrMediaNamingData
        {
            File = JoinDictionaries(data.Select(x => x.File)),
            Folder = JoinDictionaries(data.Select(x => x.Folder)),
        };
    });

    private readonly Lazy<SonarrMediaNamingData> _sonarrCache = new(() =>
    {
        // Get media naming directories from all providers
        var paths = providers.SelectMany(provider =>
            provider.GetMediaNamingPaths(SupportedServices.Sonarr)
        );

        var data = jsonLoader.LoadAllFilesAtPaths<SonarrMediaNamingData>(paths);
        return new SonarrMediaNamingData
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
    });

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    private static Dictionary<string, string> JoinDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dictionaries
    )
    {
        return dictionaries
            .SelectMany(x => x.Select(y => (y.Key, y.Value)))
            .ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public RadarrMediaNamingData GetRadarrNamingData() => _radarrCache.Value;

    public SonarrMediaNamingData GetSonarrNamingData() => _sonarrCache.Value;
}
