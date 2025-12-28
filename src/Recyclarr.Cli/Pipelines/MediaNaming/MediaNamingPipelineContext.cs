using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal class MediaNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Media Naming";
    public override bool ShouldSkip => !Plan.MediaNamingAvailable;

    public MediaNamingDto ApiFetchOutput { get; set; } = null!;
    public MediaNamingDto TransactionOutput { get; set; } = null!;
}
