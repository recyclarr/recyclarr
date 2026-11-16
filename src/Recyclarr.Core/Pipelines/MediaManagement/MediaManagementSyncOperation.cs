using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaManagement;

internal class MediaManagementSyncOperation(ILogger log, IMediaManagementService api)
    : SyncOperation<PlannedMediaManagement, MediaManagementComputeResult>
{
    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) =>
        new MediaManagementPipelineResult(SyncResultStatus.Succeeded, null).WithStatus(
            status,
            blockedBy
        );

    protected override async Task<MediaManagementComputeResult> Compute(
        PlannedMediaManagement planned,
        IReadOnlyList<SyncOutcome> planOutcomes,
        CancellationToken ct
    )
    {
        var current = await api.GetMediaManagement(ct);
        var desired = current with { PropersAndRepacks = planned.PropersAndRepacks };
        var propersAndRepacks = PlannedValue.Compared(
            current.PropersAndRepacks,
            desired.PropersAndRepacks
        );
        var settings = new PlannedMediaManagementResource
        {
            Action = propersAndRepacks.Changed ? PlannedAction.Update : PlannedAction.Unchanged,
            PropersAndRepacks = propersAndRepacks,
        };
        var result = new MediaManagementPipelineResult(SyncResultStatus.Succeeded, settings);
        return new MediaManagementComputeResult(current, desired, result);
    }

    protected override async Task Persist(
        MediaManagementComputeResult computeResult,
        CancellationToken ct
    )
    {
        var (current, desired, result) = computeResult;
        var differences = current.GetDifferences(desired);
        if (result.Settings is { Action: PlannedAction.Update })
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
