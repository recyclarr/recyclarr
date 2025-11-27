using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

internal class SonarrMediaNamingConfigPhase(SonarrConfiguration config)
    : IServiceBasedMediaNamingConfigPhase
{
    public Task<MediaNamingDto> ProcessNaming(
        MediaNamingResourceQuery guide,
        NamingFormatLookup lookup
    )
    {
        var guideData = guide.GetSonarr();
        var configData = config.MediaNaming;
        const string keySuffix = ":4";

        return Task.FromResult<MediaNamingDto>(
            new SonarrMediaNamingDto
            {
                SeasonFolderFormat = lookup.ObtainFormat(
                    guideData.Season,
                    configData.Season,
                    "Season Folder Format"
                ),
                SeriesFolderFormat = lookup.ObtainFormat(
                    guideData.Series,
                    configData.Series,
                    "Series Folder Format"
                ),
                StandardEpisodeFormat = lookup.ObtainFormat(
                    guideData.Episodes.Standard,
                    configData.Episodes?.Standard,
                    keySuffix,
                    "Standard Episode Format"
                ),
                DailyEpisodeFormat = lookup.ObtainFormat(
                    guideData.Episodes.Daily,
                    configData.Episodes?.Daily,
                    keySuffix,
                    "Daily Episode Format"
                ),
                AnimeEpisodeFormat = lookup.ObtainFormat(
                    guideData.Episodes.Anime,
                    configData.Episodes?.Anime,
                    keySuffix,
                    "Anime Episode Format"
                ),
                RenameEpisodes = configData.Episodes?.Rename,
            }
        );
    }
}
