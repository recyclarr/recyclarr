using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Pipelines;

internal interface ISyncOperation
{
    PipelineType Type { get; }
    string Description { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }
    bool ShouldSkip(PipelinePlan plan);

    Task<object?> Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct);
    Task Persist(object? computeResult, IPipelinePublisher publisher, CancellationToken ct);
}
