using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines.MediaManagement;

internal class MediaManagementSyncOperation(ILogger log, IMediaManagementService api)
    : SyncOperation<MediaManagementComputeResult>
{
    public override PipelineType Type => PipelineType.MediaManagement;
    public override string Description => "Media Management";

    public override bool ShouldSkip(PipelinePlan plan) => !plan.MediaManagementAvailable;

    protected override async Task<MediaManagementComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var current = await api.GetMediaManagement(ct);
        var planned = plan.MediaManagement;
        var desired = current with { PropersAndRepacks = planned.PropersAndRepacks };
        return new MediaManagementComputeResult(current, desired);
    }

    protected override async Task Persist(
        MediaManagementComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var (current, desired) = computeResult;
        var differences = current.GetDifferences(desired);

        if (differences.Count != 0)
        {
            await api.UpdateMediaManagement(desired, ct);
            log.Information("Media management has been updated");
            log.Debug("Media management differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media management is up to date!");
        }

        publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
    }
}
