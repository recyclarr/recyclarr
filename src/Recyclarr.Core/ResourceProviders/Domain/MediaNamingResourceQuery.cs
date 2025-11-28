using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Serilog;

namespace Recyclarr.ResourceProviders.Domain;

public class MediaNamingResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public RadarrMediaNamingResource GetRadarr()
    {
        log.Debug("MediaNaming: Querying Radarr naming conventions");
        var files = registry.Get<RadarrMediaNamingResource>();
        log.Debug("MediaNaming: Found {Count} naming files in registry", files.Count);

        var allData = loader
            .Load<RadarrMediaNamingResource>(files)
            .Select(tuple => tuple.Resource)
            .ToList();

        var result = new RadarrMediaNamingResource
        {
            File = MergeDictionaries(allData.Select(x => x.File)),
            Folder = MergeDictionaries(allData.Select(x => x.Folder)),
        };

        log.Debug(
            "MediaNaming: Retrieved {FileCount} file formats, {FolderCount} folder formats",
            result.File.Count,
            result.Folder.Count
        );
        return result;
    }

    public SonarrMediaNamingResource GetSonarr()
    {
        log.Debug("MediaNaming: Querying Sonarr naming conventions");
        var files = registry.Get<SonarrMediaNamingResource>();
        log.Debug("MediaNaming: Found {Count} naming files in registry", files.Count);

        var allData = loader
            .Load<SonarrMediaNamingResource>(files)
            .Select(tuple => tuple.Resource)
            .ToList();

        var result = new SonarrMediaNamingResource
        {
            Season = MergeDictionaries(allData.Select(x => x.Season)),
            Series = MergeDictionaries(allData.Select(x => x.Series)),
            Episodes = new SonarrEpisodeNamingResource
            {
                Anime = MergeDictionaries(allData.Select(x => x.Episodes.Anime)),
                Daily = MergeDictionaries(allData.Select(x => x.Episodes.Daily)),
                Standard = MergeDictionaries(allData.Select(x => x.Episodes.Standard)),
            },
        };

        log.Debug(
            "MediaNaming: Retrieved {SeasonCount} season, {SeriesCount} series, {AnimeCount} anime, {DailyCount} daily, {StandardCount} standard formats",
            result.Season.Count,
            result.Series.Count,
            result.Episodes.Anime.Count,
            result.Episodes.Daily.Count,
            result.Episodes.Standard.Count
        );
        return result;
    }

    private static Dictionary<string, string> MergeDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dicts
    )
    {
        return dicts
            .SelectMany(d => d)
            .GroupBy(kvp => kvp.Key.ToLowerInvariant())
            .Select(g => g.Last())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
