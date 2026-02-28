using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class RadarrMediaNamingPlanComponent(
    MediaNamingResourceQuery guide,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        if (config is not RadarrConfiguration radarrConfig)
        {
            return;
        }

        var lookup = new NamingFormatLookup();
        var dto = BuildDto(radarrConfig, guide, lookup);

        foreach (var (type, configValue) in lookup.Errors)
        {
            plan.AddError($"Invalid {type} naming format: {configValue}");
        }

        if (dto.GetDifferences(new RadarrMediaNamingDto()).Count == 0)
        {
            return;
        }

        plan.RadarrMediaNaming = new PlannedRadarrMediaNaming { Dto = dto };
    }

    private static RadarrMediaNamingDto BuildDto(
        RadarrConfiguration config,
        MediaNamingResourceQuery guide,
        NamingFormatLookup lookup
    )
    {
        var guideData = guide.GetRadarr();
        var configData = config.MediaNaming;

        return new RadarrMediaNamingDto
        {
            StandardMovieFormat = lookup.ObtainFormat(
                guideData.File,
                configData.Movie?.Standard,
                "Standard Movie Format"
            ),
            MovieFolderFormat = lookup.ObtainFormat(
                guideData.Folder,
                configData.Folder,
                "Movie Folder Format"
            ),
            RenameMovies = configData.Movie?.Rename,
        };
    }
}
