using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

// This interface allows having a collection of generic sync pipelines without needing to be aware
// of the generic parameters. PipelineType and Dependencies are bridged from static members on the
// context type via GenericSyncPipeline.
internal interface ISyncPipeline
{
    PipelineType PipelineType { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }

    Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    );
}
