using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Pipelines;

internal interface IPipelineExecutor
{
    Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IInstancePublisher instancePublisher,
        JobId jobId,
        string instanceName,
        CancellationToken ct
    );

    void InterruptAll(IInstancePublisher instancePublisher);
}
