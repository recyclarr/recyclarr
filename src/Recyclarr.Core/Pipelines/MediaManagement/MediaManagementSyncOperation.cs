using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaManagement;

internal class MediaManagementSyncOperation(ILogger log, IMediaManagementService api)
    : SyncOperation<MediaManagementComputeResult>
{
    public override PipelineType Type => PipelineType.MediaManagement;
    public override string Description => "Media Management";

    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) =>
        new MediaManagementPipelineResult(SyncResultStatus.Succeeded, null).WithStatus(
            status,
            blockedBy
        );

    public override bool ShouldSkip(PipelinePlan plan) => !plan.MediaManagementAvailable;

    protected override async Task<MediaManagementComputeResult> Compute(
        PipelinePlan plan,
        CancellationToken ct
    )
    {
        var current = await api.GetMediaManagement(ct);
        var planned = plan.MediaManagement;
        var desired = current with { PropersAndRepacks = planned.PropersAndRepacks };
        var delta =
            current.PropersAndRepacks != desired.PropersAndRepacks
                ? new MediaManagementDelta(
                    new ValueDelta<PropersAndRepacksMode?>(
                        current.PropersAndRepacks,
                        desired.PropersAndRepacks
                    )
                )
                : null;
        var result = new MediaManagementPipelineResult(SyncResultStatus.Succeeded, delta);
        return new MediaManagementComputeResult(current, desired, result);
    }

    protected override async Task Persist(
        MediaManagementComputeResult computeResult,
        CancellationToken ct
    )
    {
        var (current, desired, result) = computeResult;
        var differences = current.GetDifferences(desired);
        if (result.Delta is not null)
        {
            await api.UpdateMediaManagement(desired, ct);
            log.Information("Media management has been updated");
            log.Debug("Media management differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media management is up to date!");
        }
    }
}
