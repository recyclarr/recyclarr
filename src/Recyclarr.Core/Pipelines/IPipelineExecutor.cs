using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

internal interface IPipelineExecutor
{
    Task<IReadOnlyList<SemanticPipelineResult>> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        PipelineExecutionBuffer buffer,
        CancellationToken ct
    );
}
