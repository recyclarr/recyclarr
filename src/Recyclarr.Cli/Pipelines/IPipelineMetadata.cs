using Recyclarr.Compatibility;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

internal interface IPipelineMetadata
{
    static abstract PipelineType PipelineType { get; }
    static abstract IReadOnlyList<PipelineType> Dependencies { get; }

    // Pipelines that do not apply to the detected API target are never constructed, so their
    // phases may depend on ports that target has no gateway for.
    static abstract bool AppliesTo(ServiceApiTarget target);
}
