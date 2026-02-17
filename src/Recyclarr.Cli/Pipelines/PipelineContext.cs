using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

internal abstract class PipelineContext
{
    public abstract string PipelineDescription { get; }
    public virtual bool ShouldSkip => false;

    // Set from `GenericSyncPipeline.Execute()`. Not able to make these `required` because of the
    // `new()` constraint.
    public string InstanceName { get; init; } = null!;
    public ISyncSettings SyncSettings { get; init; } = null!;
    public IPipelinePublisher Publisher { get; init; } = IPipelinePublisher.Noop;

    // Shared plan across all pipelines. Set by sync orchestration before pipelines run.
    public PipelinePlan Plan { get; init; } = null!;
}
