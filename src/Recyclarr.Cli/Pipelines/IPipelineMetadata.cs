using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines;

internal interface IPipelineMetadata
{
    static abstract PipelineType PipelineType { get; }
    static abstract IReadOnlyList<PipelineType> Dependencies { get; }
    static abstract SupportedServices? ServiceAffinity { get; }
}
