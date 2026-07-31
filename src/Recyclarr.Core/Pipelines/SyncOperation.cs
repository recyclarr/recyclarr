using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

internal abstract class SyncOperation<TResult> : ISyncOperation
    where TResult : IPipelineResultSource
{
    public abstract PipelineType Type { get; }
    public abstract string Description { get; }
    public virtual IReadOnlyList<PipelineType> Dependencies => [];

    public virtual bool ShouldSkip(PipelinePlan plan) => false;

    async Task<SemanticPipelineResult> ISyncOperation.Execute(
        bool preview,
        PipelinePlan plan,
        IPipelinePublisher publisher,
        Action<SemanticPipelineResult> capture,
        CancellationToken ct
    )
    {
        var computeResult = await Compute(plan, publisher, ct);
        capture(computeResult.Result);

        if (!preview)
        {
            await Persist(computeResult, publisher, ct);
            capture(computeResult.Result);
        }

        return computeResult.Result;
    }

    SemanticPipelineResult ISyncOperation.CreateBlockedResult(PipelineType dependency) =>
        CreateEmptyResult(SyncResultStatus.Blocked, dependency);

    SemanticPipelineResult ISyncOperation.CreateFailedResult(SemanticPipelineResult? current) =>
        current?.WithStatus(SyncResultStatus.Failed)
        ?? CreateEmptyResult(SyncResultStatus.Failed, null);

    protected abstract SemanticPipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    );

    protected abstract Task<TResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    );

    protected abstract Task Persist(
        TResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    );
}
