using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Pipelines;

internal abstract class SyncOperation<TResult> : ISyncOperation
{
    public abstract PipelineType Type { get; }
    public abstract string Description { get; }
    public virtual IReadOnlyList<PipelineType> Dependencies => [];

    public virtual bool ShouldSkip(PipelinePlan plan) => false;

    // Bridge untyped interface contract to strongly-typed abstract methods
    async Task<object?> ISyncOperation.Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    ) => await Compute(plan, publisher, ct);

    async Task ISyncOperation.Persist(
        object? computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    ) =>
        // non-null: orchestrator always passes the result from Compute
        await Persist((TResult)computeResult!, publisher, ct);

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
