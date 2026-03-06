using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];
    public static SupportedServices? ServiceAffinity => SupportedServices.Sonarr;

    public override string PipelineDescription => "Sonarr Media Naming";
    public override bool ShouldSkip => !Plan.SonarrMediaNamingAvailable;

    public SonarrNamingData ApiFetchOutput { get; set; } = null!;
    public SonarrNamingData TransactionOutput { get; set; } = null!;
}
