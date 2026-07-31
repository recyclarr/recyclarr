using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingSyncOperation(ILogger log, IRadarrNamingService api)
    : SyncOperation<RadarrNamingComputeResult>
{
    public override PipelineType Type => PipelineType.MediaNaming;
    public override string Description => "Radarr Media Naming";

    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => new RadarrNamingPipelineResult(0, 0, [], null).WithStatus(status, blockedBy);

    public override bool ShouldSkip(PipelinePlan plan) => !plan.RadarrMediaNamingAvailable;

    protected override async Task<RadarrNamingComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var planned = plan.RadarrMediaNaming.Data;
        var outcomes = plan.RadarrMediaNaming.Mismatches.Cast<RadarrNamingOutcome>().ToList();
        var completedFields = CountConfiguredFields(planned);
        var incompleteFields = outcomes.Count;
        if (completedFields == 0)
        {
            var failedResult = new RadarrNamingPipelineResult(0, incompleteFields, outcomes, null);
            SetStatus(publisher, failedResult);
            return new RadarrNamingComputeResult(null, null, failedResult);
        }

        var current = await api.GetNaming(ct);

        // Overlay only non-null planned values; null means "don't change"
        var desired = current with
        {
            RenameMovies = planned.RenameMovies ?? current.RenameMovies,
            StandardMovieFormat = planned.StandardMovieFormat ?? current.StandardMovieFormat,
            MovieFolderFormat = planned.MovieFolderFormat ?? current.MovieFolderFormat,
        };

        var delta = BuildDelta(current, desired, planned);
        var result = new RadarrNamingPipelineResult(
            completedFields,
            incompleteFields,
            outcomes,
            delta
        );
        SetStatus(publisher, result);
        return new RadarrNamingComputeResult(current, desired, result);
    }

    protected override async Task Persist(
        RadarrNamingComputeResult computeResult,
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

    private static RadarrNamingDelta? BuildDelta(
        RadarrNamingData current,
        RadarrNamingData desired,
        RadarrNamingData planned
    )
    {
        var changes = new ConfiguredValueChanges<RadarrNamingData>(current, desired, planned);
        var delta = new RadarrNamingDelta
        {
            RenameMovies = changes.For(x => x.RenameMovies),
            StandardMovieFormat = changes.For(x => x.StandardMovieFormat),
            MovieFolderFormat = changes.For(x => x.MovieFolderFormat),
        };

        return delta == new RadarrNamingDelta() ? null : delta;
    }

    private static int CountConfiguredFields(RadarrNamingData planned)
    {
        var count = 0;
        count += planned.RenameMovies is not null ? 1 : 0;
        count += planned.StandardMovieFormat is not null ? 1 : 0;
        count += planned.MovieFolderFormat is not null ? 1 : 0;
        return count;
    }

    private static void SetStatus(
        IPipelinePublisher publisher,
        RadarrNamingPipelineResult result,
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
