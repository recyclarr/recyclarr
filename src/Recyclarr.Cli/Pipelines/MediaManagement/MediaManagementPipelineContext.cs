using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.MediaManagement;

internal class MediaManagementPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaManagement;
    public static IReadOnlyList<PipelineType> Dependencies => [];
    public static SupportedServices? ServiceAffinity => null;

    public override string PipelineDescription => "Media Management";
    public override bool ShouldSkip => !Plan.MediaManagementAvailable;

    public MediaManagementData ApiFetchOutput { get; set; } = null!;
    public MediaManagementData TransactionOutput { get; set; } = null!;
}
