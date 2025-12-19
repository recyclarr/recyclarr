using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal class MediaNamingPipelineContext : PipelineContext
{
    public override string PipelineDescription => "Media Naming";
    public override PipelineType PipelineType => PipelineType.MediaNaming;
    public override bool ShouldSkip => !Plan.MediaNamingAvailable;

    public MediaNamingDto ApiFetchOutput { get; set; } = null!;
    public MediaNamingDto TransactionOutput { get; set; } = null!;
}
