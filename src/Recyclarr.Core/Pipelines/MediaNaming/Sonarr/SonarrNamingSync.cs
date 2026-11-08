using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingSync(ILogger log, ISonarrNamingService api) : IMediaNamingSync
{
    public PipelineResult CreateEmptyResult() => new SonarrNamingPipelineResult(0, 0, [], null);

    public async Task<IPipelineResultSource> Compute(
        PlannedMediaNaming plannedNaming,
        CancellationToken ct
    )
    {
        // DI resolves this sync only for Sonarr instances, whose planner plans Sonarr naming
        var sonarrNaming = (PlannedSonarrMediaNaming)plannedNaming;
        var planned = sonarrNaming.Data;
        var outcomes = sonarrNaming.Mismatches.Cast<SonarrNamingOutcome>().ToList();
        var completedFields = CountConfiguredFields(planned);
        var incompleteFields = outcomes.Count;
        if (completedFields == 0)
        {
            var failedResult = new SonarrNamingPipelineResult(0, incompleteFields, outcomes, null);
            return new SonarrNamingComputeResult(null, null, failedResult);
        }

        var current = await api.GetNaming(ct);

        // Overlay only non-null planned values; null means "don't change"
        var desired = current with
        {
            RenameEpisodes = planned.RenameEpisodes ?? current.RenameEpisodes,
            SeriesFolderFormat = planned.SeriesFolderFormat ?? current.SeriesFolderFormat,
            SeasonFolderFormat = planned.SeasonFolderFormat ?? current.SeasonFolderFormat,
            StandardEpisodeFormat = planned.StandardEpisodeFormat ?? current.StandardEpisodeFormat,
            DailyEpisodeFormat = planned.DailyEpisodeFormat ?? current.DailyEpisodeFormat,
            AnimeEpisodeFormat = planned.AnimeEpisodeFormat ?? current.AnimeEpisodeFormat,
        };

        var settings = BuildSettings(current, desired, planned);
        var result = new SonarrNamingPipelineResult(
            completedFields,
            incompleteFields,
            outcomes,
            settings
        );
        return new SonarrNamingComputeResult(current, desired, result);
    }

    public async Task Persist(IPipelineResultSource computeResult, CancellationToken ct)
    {
        var (current, desired, result) = (SonarrNamingComputeResult)computeResult;
        if (result.Settings is not { Action: PlannedAction.Update })
        {
            return;
        }

        if (current is null || desired is null)
        {
            throw new InvalidOperationException("Changed naming result requires transaction data");
        }

        await api.UpdateNaming(desired, ct);

        var differences = current.GetDifferences(desired);

        if (differences.Count != 0)
        {
            log.Information("Media naming has been updated");
            log.Debug("Naming differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media naming is up to date!");
        }
    }

    private static PlannedSonarrNamingResource BuildSettings(
        SonarrNamingData current,
        SonarrNamingData desired,
        SonarrNamingData planned
    )
    {
        var values = new ConfiguredValues<SonarrNamingData>(current, desired, planned);
        var renameEpisodes = values.For(x => x.RenameEpisodes);
        var seriesFolderFormat = values.For(x => x.SeriesFolderFormat);
        var seasonFolderFormat = values.For(x => x.SeasonFolderFormat);
        var standardEpisodeFormat = values.For(x => x.StandardEpisodeFormat);
        var dailyEpisodeFormat = values.For(x => x.DailyEpisodeFormat);
        var animeEpisodeFormat = values.For(x => x.AnimeEpisodeFormat);

        return new PlannedSonarrNamingResource
        {
            Action = ConfiguredValues.ActionFor(
                renameEpisodes?.Changed,
                seriesFolderFormat?.Changed,
                seasonFolderFormat?.Changed,
                standardEpisodeFormat?.Changed,
                dailyEpisodeFormat?.Changed,
                animeEpisodeFormat?.Changed
            ),
            RenameEpisodes = renameEpisodes,
            SeriesFolderFormat = seriesFolderFormat,
            SeasonFolderFormat = seasonFolderFormat,
            StandardEpisodeFormat = standardEpisodeFormat,
            DailyEpisodeFormat = dailyEpisodeFormat,
            AnimeEpisodeFormat = animeEpisodeFormat,
        };
    }

    private static int CountConfiguredFields(SonarrNamingData planned)
    {
        var count = 0;
        count += planned.RenameEpisodes is not null ? 1 : 0;
        count += planned.SeriesFolderFormat is not null ? 1 : 0;
        count += planned.SeasonFolderFormat is not null ? 1 : 0;
        count += planned.StandardEpisodeFormat is not null ? 1 : 0;
        count += planned.DailyEpisodeFormat is not null ? 1 : 0;
        count += planned.AnimeEpisodeFormat is not null ? 1 : 0;
        return count;
    }
}
