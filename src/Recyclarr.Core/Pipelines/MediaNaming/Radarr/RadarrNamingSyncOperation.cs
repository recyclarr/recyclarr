using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingSyncOperation(ILogger log, IRadarrNamingService api)
    : SyncOperation<RadarrNamingComputeResult>
{
    public override PipelineType Type => PipelineType.MediaNaming;
    public override string Description => "Radarr Media Naming";

    public override bool ShouldSkip(PipelinePlan plan) => !plan.RadarrMediaNamingAvailable;

    protected override async Task<RadarrNamingComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var current = await api.GetNaming(ct);
        var planned = plan.RadarrMediaNaming.Data;

        // Overlay only non-null planned values; null means "don't change"
        var desired = current with
        {
            RenameMovies = planned.RenameMovies ?? current.RenameMovies,
            StandardMovieFormat = planned.StandardMovieFormat ?? current.StandardMovieFormat,
            MovieFolderFormat = planned.MovieFolderFormat ?? current.MovieFolderFormat,
        };

        return new RadarrNamingComputeResult(current, desired);
    }

    protected override async Task Persist(
        RadarrNamingComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var (current, desired) = computeResult;
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

        publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
    }
}
