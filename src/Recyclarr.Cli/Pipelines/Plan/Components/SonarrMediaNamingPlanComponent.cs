using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class SonarrMediaNamingPlanComponent(
    MediaNamingResourceQuery guide,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        if (config is not SonarrConfiguration sonarrConfig)
        {
            return;
        }

        var lookup = new NamingFormatLookup();
        var dto = BuildDto(sonarrConfig, guide, lookup);

        foreach (var (type, configValue) in lookup.Errors)
        {
            plan.AddError($"Invalid {type} naming format: {configValue}");
        }

        if (dto.GetDifferences(new SonarrMediaNamingDto()).Count == 0)
        {
            return;
        }

        plan.SonarrMediaNaming = new PlannedSonarrMediaNaming { Dto = dto };
    }

    private static SonarrMediaNamingDto BuildDto(
        SonarrConfiguration config,
        MediaNamingResourceQuery guide,
        NamingFormatLookup lookup
    )
    {
        var guideData = guide.GetSonarr();
        var configData = config.MediaNaming;
        const string keySuffix = ":4";

        return new SonarrMediaNamingDto
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
        };
    }
}
