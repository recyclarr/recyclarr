using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Json.Loading;
using Recyclarr.Repo;

namespace Recyclarr.TrashGuide.MediaNaming;

public class MediaNamingGuideService(IRepoMetadataBuilder metadataBuilder, GuideJsonLoader jsonLoader)
    : IMediaNamingGuideService
{
    private IReadOnlyList<IDirectoryInfo> CreatePaths(SupportedServices serviceType)
    {
        var metadata = metadataBuilder.GetMetadata();
        return serviceType switch
        {
            SupportedServices.Radarr => metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Radarr.Naming),
            SupportedServices.Sonarr => metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.Naming),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
        };
    }

    private static Dictionary<string, string> JoinDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dictionaries)
    {
        return dictionaries
            .SelectMany(x => x.Select(y => (y.Key, y.Value)))
            .ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public RadarrMediaNamingData GetRadarrNamingData()
    {
        var paths = CreatePaths(SupportedServices.Radarr);
        var data = jsonLoader.LoadAllFilesAtPaths<RadarrMediaNamingData>(paths);
        return new RadarrMediaNamingData
        {
            File = JoinDictionaries(data.Select(x => x.File)),
            Folder = JoinDictionaries(data.Select(x => x.Folder))
        };
    }

    public SonarrMediaNamingData GetSonarrNamingData()
    {
        var paths = CreatePaths(SupportedServices.Sonarr);
        var data = jsonLoader.LoadAllFilesAtPaths<SonarrMediaNamingData>(paths);
        return new SonarrMediaNamingData
        {
            Season = JoinDictionaries(data.Select(x => x.Season)),
            Series = JoinDictionaries(data.Select(x => x.Series)),
            Episodes = new SonarrEpisodeNamingData
            {
                Anime = JoinDictionaries(data.Select(x => x.Episodes.Anime)),
                Daily = JoinDictionaries(data.Select(x => x.Episodes.Daily)),
                Standard = JoinDictionaries(data.Select(x => x.Episodes.Standard))
            }
        };
    }
}
