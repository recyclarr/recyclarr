using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public class MediaNamingPipelineContext
{
    public ProcessedNamingConfig ConfigOutput { get; set; } = default!;
    public MediaNamingDto ApiFetchOutput { get; set; } = default!;
    public MediaNamingDto TransactionOutput { get; set; } = default!;
}
