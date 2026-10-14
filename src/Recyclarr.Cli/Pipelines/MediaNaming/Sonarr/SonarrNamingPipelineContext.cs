using Recyclarr.Compatibility;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaNaming;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public static bool AppliesTo(ServiceApiTarget target) =>
        target is ServiceApiTarget.SonarrV3 or ServiceApiTarget.SonarrV5;

    public override string PipelineDescription => "Sonarr Media Naming";
    public override bool ShouldSkip => !Plan.SonarrMediaNamingAvailable;

    public SonarrNamingData ApiFetchOutput { get; set; } = null!;
    public SonarrNamingData TransactionOutput { get; set; } = null!;
}
