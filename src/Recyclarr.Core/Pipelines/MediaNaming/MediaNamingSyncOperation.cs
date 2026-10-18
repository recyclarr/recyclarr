using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming;

/// <summary>
/// Syncs media naming for the instance's service. Sonarr and Radarr naming share no fields, so
/// all computing and persisting lives in the <see cref="IMediaNamingSync"/> that DI selects
/// (ADR-023).
/// </summary>
internal class MediaNamingSyncOperation(IMediaNamingSync sync)
    : SyncOperation<PlannedMediaNaming, IPipelineResultSource>
{
    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => sync.CreateEmptyResult().WithStatus(status, blockedBy);

    protected override Task<IPipelineResultSource> Compute(
        PlannedMediaNaming plan,
        IReadOnlyList<SyncOutcome> planOutcomes,
        CancellationToken ct
    ) => sync.Compute(plan, ct);

    protected override Task Persist(IPipelineResultSource computeResult, CancellationToken ct) =>
        sync.Persist(computeResult, ct);
}

/// <summary>
/// One service's media naming sync. <see cref="Persist"/> receives the result that
/// <see cref="Compute"/> of the same implementation returned.
/// </summary>
internal interface IMediaNamingSync
{
    PipelineResult CreateEmptyResult();
    Task<IPipelineResultSource> Compute(PlannedMediaNaming planned, CancellationToken ct);
    Task Persist(IPipelineResultSource computeResult, CancellationToken ct);
}
