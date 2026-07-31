using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Pipelines;

internal interface IPipelineExecutor
{
    Task<IReadOnlyList<SemanticPipelineResult>> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IInstancePublisher instancePublisher,
        PipelineExecutionBuffer buffer,
        CancellationToken ct
    );

    void InterruptAll(IInstancePublisher instancePublisher);
}
