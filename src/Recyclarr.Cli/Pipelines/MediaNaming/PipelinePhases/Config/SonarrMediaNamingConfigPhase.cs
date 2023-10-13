using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

public class SonarrMediaNamingConfigPhase(ISonarrCapabilityFetcher sonarrCapabilities)
    : ServiceBasedMediaNamingConfigPhase<SonarrConfiguration>
{
    protected override async Task<MediaNamingDto> ProcessNaming(
        SonarrConfiguration config,
        IMediaNamingGuideService guide,
        NamingFormatLookup lookup)
    {
        var guideData = guide.GetSonarrNamingData();
        var configData = config.MediaNaming;
        var capabilities = await sonarrCapabilities.GetCapabilities(config);
        var keySuffix = capabilities.SupportsCustomFormats ? ":4" : ":3";

        return new SonarrMediaNamingDto
        {
            SeasonFolderFormat = lookup.ObtainFormat(guideData.Season, configData.Season, "Season Folder Format"),
            SeriesFolderFormat = lookup.ObtainFormat(guideData.Series, configData.Series, "Series Folder Format"),
            StandardEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Standard,
                configData.Episodes?.Standard,
                keySuffix,
                "Standard Episode Format"),
            DailyEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Daily,
                configData.Episodes?.Daily,
                keySuffix,
                "Daily Episode Format"),
            AnimeEpisodeFormat = lookup.ObtainFormat(
                guideData.Episodes.Anime,
                configData.Episodes?.Anime,
                keySuffix,
                "Anime Episode Format"),
            RenameEpisodes = configData.Episodes?.Rename
        };
    }
}
