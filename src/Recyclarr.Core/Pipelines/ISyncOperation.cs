using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

internal interface ISyncOperation
{
    PipelineType Type { get; }
    string Description { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }
    bool ShouldSkip(PipelinePlan plan);

    Task<SemanticPipelineResult> Execute(
        bool preview,
        PipelinePlan plan,
        IPipelinePublisher publisher,
        Action<SemanticPipelineResult> capture,
        CancellationToken ct
    );

    SemanticPipelineResult CreateBlockedResult(PipelineType dependency);
    SemanticPipelineResult CreateFailedResult(SemanticPipelineResult? current);
}
