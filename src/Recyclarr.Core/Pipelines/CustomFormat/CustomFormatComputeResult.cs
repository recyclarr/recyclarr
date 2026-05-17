using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.CustomFormat;

internal record CustomFormatComputeResult(
    CustomFormatTransactionData Transactions,
    IEnumerable<int> ValidServiceIds,
    TrashIdMappingStore State,
    IReadOnlyDictionary<string, CustomFormatSourceInfo> SourceInfo
) : ISyncStateSource
{
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        Transactions
            .NewCustomFormats.Concat(Transactions.UpdatedCustomFormats)
            .Concat(Transactions.UnchangedCustomFormats)
            .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id));

    public IEnumerable<int> DeletedIds =>
        Transactions.DeletedCustomFormats.Select(m => m.ServiceId);
}
