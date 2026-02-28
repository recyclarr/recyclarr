using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.MediaManagement;

internal class MediaManagementPipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.MediaManagement;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Media Management";
    public override bool ShouldSkip => Plan.MediaManagement is null;

    public MediaManagementData ApiFetchOutput { get; set; } = null!;
    public MediaManagementData TransactionOutput { get; set; } = null!;
}
