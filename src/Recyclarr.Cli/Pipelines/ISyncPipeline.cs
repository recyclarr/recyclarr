using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines;

// This interface allows having a collection of generic sync pipelines without needing to be aware
// of the generic parameters. PipelineType and Dependencies are bridged from static members on the
// context type via GenericSyncPipeline.
internal interface ISyncPipeline
{
    PipelineType PipelineType { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }
    bool AppliesTo(SupportedServices serviceType);

    Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    );
}
