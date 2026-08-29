using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.CustomFormat;

internal sealed class CustomFormatComputeResult(
    CustomFormatTransactionData transactions,
    IEnumerable<int> validServiceIds,
    TrashIdMappingStore state,
    IReadOnlyDictionary<string, CustomFormatSourceInfo> sourceInfo,
    CustomFormatPipelineResult result,
    int transactionIncompleteResources
) : ISyncStateSource, IPipelineResultSource
{
    private readonly HashSet<int> _validServiceIds = validServiceIds.ToHashSet();
    private readonly List<TrashIdMapping> _syncedMappings = transactions
        .UnchangedCustomFormats.Select(ToMapping)
        .Where(x => x.ServiceId != 0)
        .ToList();
    private readonly HashSet<int> _deletedIds = [];

    public CustomFormatTransactionData Transactions { get; } = transactions;
    public TrashIdMappingStore State { get; } = state;
    public IReadOnlyDictionary<string, CustomFormatSourceInfo> SourceInfo { get; } = sourceInfo;
    public CustomFormatPipelineResult Result { get; private set; } = result;
    public int TransactionIncompleteResources { get; } = transactionIncompleteResources;

    Recyclarr.Sync.Results.PipelineResult IPipelineResultSource.Result => Result;

    public IEnumerable<int> ValidServiceIds => _validServiceIds;
    public IEnumerable<TrashIdMapping> SyncedMappings => _syncedMappings;
    public IEnumerable<int> DeletedIds => _deletedIds;

    public void RecordSynced(CustomFormatResource cf)
    {
        if (cf.Id == 0)
        {
            return;
        }

        _validServiceIds.Add(cf.Id);
        _syncedMappings.Add(ToMapping(cf));
    }

    public void RecordDeleted(int serviceId)
    {
        _deletedIds.Add(serviceId);
    }

    public void CompleteApply(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<CustomFormatOutcome> persistenceOutcomes
    )
    {
        Result = new CustomFormatPipelineResult(
            completedResources,
            incompleteResources,
            [.. Result.Outcomes, .. persistenceOutcomes],
            Result.Deltas
        );
    }

    private static TrashIdMapping ToMapping(CustomFormatResource cf)
    {
        return new TrashIdMapping(cf.TrashId, cf.Name, cf.Id);
    }
}
