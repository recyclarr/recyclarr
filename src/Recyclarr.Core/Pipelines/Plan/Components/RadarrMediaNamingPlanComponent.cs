using Recyclarr.Config.Models;
using Recyclarr.Pipelines.MediaNaming;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.Plan.Components;

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

        var mismatches = new List<RadarrNamingReferenceMismatchOutcome>();
        var data = BuildData(radarrConfig, guide, mismatches, plan);

        if (!data.HasValues() && mismatches.Count == 0)
        {
            return;
        }

        plan.RadarrMediaNaming = new PlannedRadarrMediaNaming
        {
            Data = data,
            Mismatches = mismatches.AsReadOnly(),
        };
    }

    private static RadarrNamingData BuildData(
        RadarrConfiguration config,
        MediaNamingResourceQuery guide,
        List<RadarrNamingReferenceMismatchOutcome> mismatches,
        PipelinePlan plan
    )
    {
        var guideData = guide.GetRadarr();
        var configData = config.MediaNaming;

        string? Resolve(
            IReadOnlyDictionary<string, string> guideFormats,
            string? configuredKey,
            RadarrNamingFormatField field
        )
        {
            var value = NamingFormatLookup.ObtainFormat(guideFormats, configuredKey);
            if (configuredKey is null || value is not null)
            {
                return value;
            }

            mismatches.Add(new RadarrNamingReferenceMismatchOutcome(field, configuredKey));
            plan.Add(new InvalidNamingFormatOutcome(DisplayName(field), configuredKey));
            return null;
        }

        return new RadarrNamingData
        {
            StandardMovieFormat = Resolve(
                guideData.File,
                configData.Movie?.Standard,
                RadarrNamingFormatField.StandardMovieFormat
            ),
            MovieFolderFormat = Resolve(
                guideData.Folder,
                configData.Folder,
                RadarrNamingFormatField.MovieFolderFormat
            ),
            RenameMovies = configData.Movie?.Rename,
        };
    }

    private static string DisplayName(RadarrNamingFormatField field) =>
        field switch
        {
            RadarrNamingFormatField.StandardMovieFormat => "Standard Movie Format",
            RadarrNamingFormatField.MovieFolderFormat => "Movie Folder Format",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };
}
