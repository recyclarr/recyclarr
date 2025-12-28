using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines;

internal interface IPipelineMetadata
{
    static abstract PipelineType PipelineType { get; }
    static abstract IReadOnlyList<PipelineType> Dependencies { get; }
}
