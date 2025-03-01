using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal class MediaNamingPipelineContext : PipelineContext
{
    public override string PipelineDescription => "Media Naming";

    public ProcessedNamingConfig ConfigOutput { get; set; } = null!;
    public MediaNamingDto ApiFetchOutput { get; set; } = null!;
    public MediaNamingDto TransactionOutput { get; set; } = null!;
}
