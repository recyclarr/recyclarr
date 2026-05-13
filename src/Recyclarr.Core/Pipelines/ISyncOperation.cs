using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Pipelines;

internal interface ISyncOperation
{
    PipelineType Type { get; }
    string Description { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }
    bool ShouldSkip(PipelinePlan plan, SupportedServices serviceType);

    Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct);
    Task Persist(IPipelinePublisher publisher, CancellationToken ct);
    void RenderPreview(string instanceName);
}
