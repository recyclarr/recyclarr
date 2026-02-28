using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.SonarrMediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Sonarr Media Naming";
    public override bool ShouldSkip => !Plan.SonarrMediaNamingAvailable;

    public SonarrMediaNamingDto ApiFetchOutput { get; set; } = null!;
    public SonarrMediaNamingDto TransactionOutput { get; set; } = null!;
}
