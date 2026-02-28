using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.RadarrMediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Radarr Media Naming";
    public override bool ShouldSkip => Plan.RadarrMediaNaming is null;

    public RadarrMediaNamingDto ApiFetchOutput { get; set; } = null!;
    public RadarrMediaNamingDto TransactionOutput { get; set; } = null!;
}
