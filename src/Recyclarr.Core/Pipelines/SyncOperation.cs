using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

internal abstract class SyncOperation<TPlan, TResult> : ISyncOperation<TPlan>
    where TResult : IPipelineResultSource
{
    async Task<SemanticPipelineResult> ISyncOperation<TPlan>.Execute(
        bool preview,
        TPlan plan,
        IReadOnlyList<SyncOutcome> planOutcomes,
        Action<SemanticPipelineResult> capture,
        CancellationToken ct
    )
    {
        var computeResult = await Compute(plan, planOutcomes, ct);
        capture(computeResult.Result);

        if (!preview)
        {
            await Persist(computeResult, ct);
            capture(computeResult.Result);
        }

        return computeResult.Result;
    }

    SemanticPipelineResult ISyncOperation<TPlan>.CreateBlockedResult(PipelineType dependency) =>
        CreateEmptyResult(SyncResultStatus.Blocked, dependency);

    SemanticPipelineResult ISyncOperation<TPlan>.CreateFailedResult(
        SemanticPipelineResult? current
    ) =>
        current?.WithStatus(SyncResultStatus.Failed)
        ?? CreateEmptyResult(SyncResultStatus.Failed, null);

    protected abstract SemanticPipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    );

    protected abstract Task<TResult> Compute(
        TPlan plan,
        IReadOnlyList<SyncOutcome> planOutcomes,
        CancellationToken ct
    );

    protected abstract Task Persist(TResult computeResult, CancellationToken ct);
}
