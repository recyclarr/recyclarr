using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

internal interface IPipelineExecutor
{
    Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IInstancePublisher instancePublisher,
        CancellationToken ct
    );

    void InterruptAll(IInstancePublisher instancePublisher);
}
