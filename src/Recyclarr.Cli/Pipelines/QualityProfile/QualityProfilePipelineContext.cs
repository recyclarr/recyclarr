using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Sync;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfilePipelineContext : PipelineContext, ISyncStateSource, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.QualityProfile;
    public static IReadOnlyList<PipelineType> Dependencies => [PipelineType.CustomFormat];
    public static SupportedServices? ServiceAffinity => null;

    public override string PipelineDescription => "Quality Profile";

    public QualityProfileServiceData ApiFetchOutput { get; set; } = null!;
    public QualityProfileTransactionData TransactionOutput { get; set; } = null!;
    public TrashIdMappingStore State { get; set; } = null!;

    // ISyncStateSource implementation
    // Only store guide-backed profiles (those with a valid service ID).
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        TransactionOutput
            .NewProfiles.Concat(TransactionOutput.UnchangedProfiles)
            .Concat(TransactionOutput.UpdatedProfiles.Select(x => x.Profile))
            .Select(ToMapping)
            .OfType<TrashIdMapping>();

    private static TrashIdMapping? ToMapping(UpdatedQualityProfile p) =>
        p
            is {
                Profile.Id: { } serviceId,
                ProfileConfig: PlannedQualityProfile.GuideBacked guideBacked,
            }
            ? new TrashIdMapping(guideBacked.Resource.TrashId, p.ProfileName, serviceId)
            : null;

    // QP has no delete flag - entries removed only when service ID no longer exists
    public IEnumerable<int> DeletedIds => [];

    public IEnumerable<int> ValidServiceIds =>
        ApiFetchOutput.Profiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value);
}
