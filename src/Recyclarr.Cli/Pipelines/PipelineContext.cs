using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Pipelines;

internal abstract class PipelineContext
{
    public abstract string PipelineDescription { get; }

    // Set from `GenericSyncPipeline.Execute()`. Not able to make this `required` because of the
    // `new()` constraint.
    public ISyncSettings SyncSettings { get; init; } = null!;
}
