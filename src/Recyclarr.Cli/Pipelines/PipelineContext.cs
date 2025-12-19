using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

internal abstract class PipelineContext
{
    public abstract string PipelineDescription { get; }
    public abstract PipelineType PipelineType { get; }
    public virtual bool ShouldSkip => false;

    // Set from `GenericSyncPipeline.Execute()`. Not able to make this `required` because of the
    // `new()` constraint.
    public ISyncSettings SyncSettings { get; init; } = null!;

    // Shared plan across all pipelines. Set by sync orchestration before pipelines run.
    public PipelinePlan Plan { get; init; } = null!;
}
