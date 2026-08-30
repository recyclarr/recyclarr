using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile;

internal sealed class QualityProfileComputeResult(
    QualityProfileTransactionData transactions,
    IEnumerable<int> validServiceIds,
    TrashIdMappingStore state,
    QualityProfilePipelineResult result,
    int transactionIncompleteResources
) : ISyncStateSource, IPipelineResultSource
{
    private readonly HashSet<int> _validServiceIds = validServiceIds.ToHashSet();
    private readonly List<TrashIdMapping> _syncedMappings = transactions
        .UnchangedProfiles.Select(ToMapping)
        .OfType<TrashIdMapping>()
        .ToList();

    public QualityProfileTransactionData Transactions { get; } = transactions;
    public TrashIdMappingStore State { get; } = state;
    public QualityProfilePipelineResult Result { get; private set; } = result;
    public int TransactionIncompleteResources { get; } = transactionIncompleteResources;

    Recyclarr.Sync.Results.PipelineResult IPipelineResultSource.Result => Result;

    public IEnumerable<int> ValidServiceIds => _validServiceIds;
    public IEnumerable<TrashIdMapping> SyncedMappings => _syncedMappings;
    public IEnumerable<int> DeletedIds => [];

    public void RecordSynced(UpdatedQualityProfile profile)
    {
        if (profile.Profile.Id is not { } serviceId)
        {
            return;
        }

        _validServiceIds.Add(serviceId);
        var mapping = ToMapping(profile);
        if (mapping is not null)
        {
            _syncedMappings.Add(mapping);
        }
    }

    public void CompleteApply(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualityProfileOutcome> persistenceOutcomes
    )
    {
        Result = new QualityProfilePipelineResult(
            completedResources,
            incompleteResources,
            [.. Result.Outcomes, .. persistenceOutcomes],
            Result.Deltas
        );
    }

    private static TrashIdMapping? ToMapping(UpdatedQualityProfile profile)
    {
        return
            profile
                is {
                    Profile.Id: { } serviceId,
                    ProfileConfig: PlannedQualityProfile.GuideBacked guideBacked,
                }
            ? new TrashIdMapping(guideBacked.Resource.TrashId, profile.EffectiveName, serviceId)
            : null;
    }
}
