using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Sync;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfilePipelineContext : PipelineContext, ISyncStateSource, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.QualityProfile;
    public static IReadOnlyList<PipelineType> Dependencies => [PipelineType.CustomFormat];

    public override string PipelineDescription => "Quality Profile";

    public QualityProfileServiceData ApiFetchOutput { get; set; } = null!;
    public QualityProfileTransactionData TransactionOutput { get; set; } = null!;
    public TrashIdMappingStore State { get; set; } = null!;

    // ISyncStateSource implementation
    // Only store guide-backed profiles (those with TrashId and valid service ID)
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        TransactionOutput
            .NewProfiles.Concat(TransactionOutput.UnchangedProfiles)
            .Concat(TransactionOutput.UpdatedProfiles.Select(x => x.Profile))
            .Where(p => p.TrashId is not null && p.ProfileDto.Id is not null)
            .Select(p => new TrashIdMapping(p.TrashId!, p.ProfileName, p.ProfileDto.Id!.Value));

    // QP has no delete flag - entries removed only when service ID no longer exists
    public IEnumerable<int> DeletedIds => [];

    public IEnumerable<int> ValidServiceIds =>
        ApiFetchOutput.Profiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value);
}
