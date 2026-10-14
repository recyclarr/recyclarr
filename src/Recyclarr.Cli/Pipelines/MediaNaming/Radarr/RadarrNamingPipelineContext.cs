using Recyclarr.Compatibility;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public static bool AppliesTo(ServiceApiTarget target) => target == ServiceApiTarget.RadarrV3;

    public override string PipelineDescription => "Radarr Media Naming";
    public override bool ShouldSkip => !Plan.RadarrMediaNamingAvailable;

    public RadarrNamingData ApiFetchOutput { get; set; } = null!;
    public RadarrNamingData TransactionOutput { get; set; } = null!;
}
