using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

public class SonarrMediaNamingConfigPhase : ServiceBasedMediaNamingConfigPhase<SonarrConfiguration>
{
    protected override Task<MediaNamingDto> ProcessNaming(
        SonarrConfiguration config,
        IMediaNamingGuideService guide,
        NamingFormatLookup lookup)
    {
        var guideData = guide.GetSonarrNamingData();
        var configData = config.MediaNaming;

        return Task.FromResult<MediaNamingDto>(new SonarrMediaNamingDto
        {
            SeasonFolderFormat = lookup.ObtainFormat(guideData.Season, configData.Season, "Season Folder Format"),
            SeriesFolderFormat = lookup.ObtainFormat(guideData.Series, configData.Series, "Series Folder Format"),
            StandardEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Standard,
                configData.Episodes?.Standard,
                "Standard Episode Format"),
            DailyEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Daily,
                configData.Episodes?.Daily,
                "Daily Episode Format"),
            AnimeEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Anime,
                configData.Episodes?.Anime,
                "Anime Episode Format"),
            RenameEpisodes = configData.Episodes?.Rename
        });
    }
}
