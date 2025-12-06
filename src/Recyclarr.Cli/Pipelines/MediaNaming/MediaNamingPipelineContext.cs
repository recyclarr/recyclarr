using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal class MediaNamingPipelineContext : PipelineContext
{
    public override string PipelineDescription => "Media Naming";

    public MediaNamingDto ApiFetchOutput { get; set; } = null!;
    public MediaNamingDto TransactionOutput { get; set; } = null!;
}
