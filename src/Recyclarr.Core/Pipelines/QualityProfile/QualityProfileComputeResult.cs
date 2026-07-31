using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile;

internal record QualityProfileComputeResult(
    QualityProfileTransactionData Transactions,
    IEnumerable<int> ValidServiceIds,
    TrashIdMappingStore State
) : ISyncStateSource
{
    // Only store guide-backed profiles (those with a valid service ID).
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        Transactions
            .NewProfiles.Concat(Transactions.UnchangedProfiles)
            .Concat(Transactions.UpdatedProfiles.Select(x => x.Profile))
            .Select(ToMapping)
            .OfType<TrashIdMapping>();

    // QP has no delete flag - entries removed only when service ID no longer exists
    public IEnumerable<int> DeletedIds => [];

    private static TrashIdMapping? ToMapping(UpdatedQualityProfile p) =>
        p
            is {
                Profile.Id: { } serviceId,
                ProfileConfig: PlannedQualityProfile.GuideBacked guideBacked,
            }
            ? new TrashIdMapping(guideBacked.Resource.TrashId, p.ProfileName, serviceId)
            : null;
}
