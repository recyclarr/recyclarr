using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.MediaNaming;

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
        var data = BuildData(radarrConfig, guide, lookup);

        foreach (var (type, configValue) in lookup.Errors)
        {
            plan.AddError($"Invalid {type} naming format: {configValue}");
        }

        if (!data.HasValues())
        {
            return;
        }

        plan.RadarrMediaNaming = new PlannedRadarrMediaNaming { Data = data };
    }

    private static RadarrNamingData BuildData(
        RadarrConfiguration config,
        MediaNamingResourceQuery guide,
        NamingFormatLookup lookup
    )
    {
        var guideData = guide.GetRadarr();
        var configData = config.MediaNaming;

        return new RadarrNamingData
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
