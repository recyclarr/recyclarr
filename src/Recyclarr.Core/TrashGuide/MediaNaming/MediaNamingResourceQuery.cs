using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.TrashGuide.MediaNaming;

public class MediaNamingResourceQuery(
    RadarrMediaNamingResourceQuery radarrQuery,
    SonarrMediaNamingResourceQuery sonarrQuery
) : IMediaNamingResourceQuery
{
    public RadarrMediaNamingData GetRadarrNamingData()
    {
        var resource = radarrQuery.GetNaming();
        return new RadarrMediaNamingData { File = resource.File, Folder = resource.Folder };
    }

    public SonarrMediaNamingData GetSonarrNamingData()
    {
        var resource = sonarrQuery.GetNaming();
        return new SonarrMediaNamingData
        {
            Season = resource.Season,
            Series = resource.Series,
            Episodes = new SonarrEpisodeNamingData
            {
                Anime = resource.Episodes.Anime,
                Daily = resource.Episodes.Daily,
                Standard = resource.Episodes.Standard,
            },
        };
    }
}
