using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.SonarrMediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Sonarr Media Naming";
    public override bool ShouldSkip => !Plan.SonarrMediaNamingAvailable;

    public SonarrNamingData ApiFetchOutput { get; set; } = null!;
    public SonarrNamingData TransactionOutput { get; set; } = null!;
}
