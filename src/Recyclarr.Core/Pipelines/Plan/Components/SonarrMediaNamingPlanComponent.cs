using Recyclarr.Config.Models;
using Recyclarr.Pipelines.MediaNaming;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.Plan.Components;

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

        var mismatches = new List<SonarrNamingReferenceMismatchOutcome>();
        var data = BuildData(sonarrConfig, guide, mismatches, plan);

        if (!data.HasValues() && mismatches.Count == 0)
        {
            return;
        }

        plan.SonarrMediaNaming = new PlannedSonarrMediaNaming
        {
            Data = data,
            Mismatches = mismatches.AsReadOnly(),
        };
    }

    private static SonarrNamingData BuildData(
        SonarrConfiguration config,
        MediaNamingResourceQuery guide,
        List<SonarrNamingReferenceMismatchOutcome> mismatches,
        PipelinePlan plan
    )
    {
        var guideData = guide.GetSonarr();
        var configData = config.MediaNaming;
        const string keySuffix = ":4";

        string? Resolve(
            IReadOnlyDictionary<string, string> guideFormats,
            string? configuredKey,
            SonarrNamingFormatField field,
            string? suffix = null
        )
        {
            var value = NamingFormatLookup.ObtainFormat(guideFormats, configuredKey, suffix);
            if (configuredKey is null || value is not null)
            {
                return value;
            }

            mismatches.Add(new SonarrNamingReferenceMismatchOutcome(field, configuredKey));
            plan.Add(new InvalidNamingFormatOutcome(DisplayName(field), configuredKey));
            return null;
        }

        return new SonarrNamingData
        {
            SeasonFolderFormat = Resolve(
                guideData.Season,
                configData.Season,
                SonarrNamingFormatField.SeasonFolderFormat
            ),
            SeriesFolderFormat = Resolve(
                guideData.Series,
                configData.Series,
                SonarrNamingFormatField.SeriesFolderFormat
            ),
            StandardEpisodeFormat = Resolve(
                guideData.Episodes.Standard,
                configData.Episodes?.Standard,
                SonarrNamingFormatField.StandardEpisodeFormat,
                keySuffix
            ),
            DailyEpisodeFormat = Resolve(
                guideData.Episodes.Daily,
                configData.Episodes?.Daily,
                SonarrNamingFormatField.DailyEpisodeFormat,
                keySuffix
            ),
            AnimeEpisodeFormat = Resolve(
                guideData.Episodes.Anime,
                configData.Episodes?.Anime,
                SonarrNamingFormatField.AnimeEpisodeFormat,
                keySuffix
            ),
            RenameEpisodes = configData.Episodes?.Rename,
        };
    }

    private static string DisplayName(SonarrNamingFormatField field) =>
        field switch
        {
            SonarrNamingFormatField.SeriesFolderFormat => "Series Folder Format",
            SonarrNamingFormatField.SeasonFolderFormat => "Season Folder Format",
            SonarrNamingFormatField.StandardEpisodeFormat => "Standard Episode Format",
            SonarrNamingFormatField.DailyEpisodeFormat => "Daily Episode Format",
            SonarrNamingFormatField.AnimeEpisodeFormat => "Anime Episode Format",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };
}
