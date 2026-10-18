using Recyclarr.Sync;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

/// <summary>
/// The Transaction and Persistence stages of one pipeline. The orchestrator calls
/// <see cref="Execute"/> only when the pipeline has a plan and nothing blocks it.
/// <c>planOutcomes</c> are the outcomes this pipeline's planner reported.
/// </summary>
internal interface ISyncOperation<in TPlan>
{
    Task<SemanticPipelineResult> Execute(
        bool preview,
        TPlan plan,
        IReadOnlyList<SyncOutcome> planOutcomes,
        Action<SemanticPipelineResult> capture,
        CancellationToken ct
    );

    SemanticPipelineResult CreateBlockedResult(PipelineType dependency);
    SemanticPipelineResult CreateFailedResult(SemanticPipelineResult? current);
}
