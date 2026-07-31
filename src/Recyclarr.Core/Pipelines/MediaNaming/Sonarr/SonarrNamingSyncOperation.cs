using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingSyncOperation(ILogger log, ISonarrNamingService api)
    : SyncOperation<SonarrNamingComputeResult>
{
    public override PipelineType Type => PipelineType.MediaNaming;
    public override string Description => "Sonarr Media Naming";

    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => new SonarrNamingPipelineResult(0, 0, [], null).WithStatus(status, blockedBy);

    public override bool ShouldSkip(PipelinePlan plan) => !plan.SonarrMediaNamingAvailable;

    protected override async Task<SonarrNamingComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var planned = plan.SonarrMediaNaming.Data;
        var outcomes = plan.SonarrMediaNaming.Mismatches.Cast<SonarrNamingOutcome>().ToList();
        var completedFields = CountConfiguredFields(planned);
        var incompleteFields = outcomes.Count;
        if (completedFields == 0)
        {
            var failedResult = new SonarrNamingPipelineResult(0, incompleteFields, outcomes, null);
            SetStatus(publisher, failedResult);
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

        var delta = BuildDelta(current, desired, planned);
        var result = new SonarrNamingPipelineResult(
            completedFields,
            incompleteFields,
            outcomes,
            delta
        );
        SetStatus(publisher, result);
        return new SonarrNamingComputeResult(current, desired, result);
    }

    protected override async Task Persist(
        SonarrNamingComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var (current, desired, result) = computeResult;
        if (result.Delta is null)
        {
            SetStatus(publisher, result, 0);
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

        SetStatus(publisher, result, differences.Count);
    }

    private static SonarrNamingDelta? BuildDelta(
        SonarrNamingData current,
        SonarrNamingData desired,
        SonarrNamingData planned
    )
    {
        var changes = new ConfiguredValueChanges<SonarrNamingData>(current, desired, planned);
        var delta = new SonarrNamingDelta
        {
            RenameEpisodes = changes.For(x => x.RenameEpisodes),
            SeriesFolderFormat = changes.For(x => x.SeriesFolderFormat),
            SeasonFolderFormat = changes.For(x => x.SeasonFolderFormat),
            StandardEpisodeFormat = changes.For(x => x.StandardEpisodeFormat),
            DailyEpisodeFormat = changes.For(x => x.DailyEpisodeFormat),
            AnimeEpisodeFormat = changes.For(x => x.AnimeEpisodeFormat),
        };

        return delta == new SonarrNamingDelta() ? null : delta;
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

    private static void SetStatus(
        IPipelinePublisher publisher,
        SonarrNamingPipelineResult result,
        int? count = null
    )
    {
        var status = result.Status switch
        {
            SyncResultStatus.Succeeded => PipelineProgressStatus.Succeeded,
            SyncResultStatus.Partial => PipelineProgressStatus.Partial,
            SyncResultStatus.Failed => PipelineProgressStatus.Failed,
            SyncResultStatus.Blocked => PipelineProgressStatus.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
        publisher.SetStatus(status, count);
    }
}
