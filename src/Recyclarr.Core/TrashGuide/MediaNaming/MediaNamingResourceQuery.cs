using System.Diagnostics.CodeAnalysis;
using Recyclarr.Json.Loading;

namespace Recyclarr.TrashGuide.MediaNaming;

public class MediaNamingResourceQuery(
    IEnumerable<IMediaNamingResourceProvider> providers,
    GuideJsonLoader jsonLoader
) : IMediaNamingGuideService
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    private static Dictionary<string, string> JoinDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dictionaries
    )
    {
        return dictionaries
            .SelectMany(x => x.Select(y => (y.Key, y.Value)))
            .ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public RadarrMediaNamingData GetRadarrNamingData()
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
    }

    public SonarrMediaNamingData GetSonarrNamingData()
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
    }
}
