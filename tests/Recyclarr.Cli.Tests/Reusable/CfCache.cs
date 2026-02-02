using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class CfCache
{
    public static TrashIdMappingStore<CustomFormatMappings> New(params TrashIdMapping[] mappings)
    {
        return new TrashIdMappingStore<CustomFormatMappings>(
            new CustomFormatMappings { Mappings = mappings.ToList() }
        );
    }

    public static ISyncStateSource ToSyncSource(
        this CustomFormatTransactionData transactions,
        IEnumerable<CustomFormatResource> serviceCfs
    )
    {
        return new TestCacheSyncSource(
            transactions
                .NewCustomFormats.Concat(transactions.UpdatedCustomFormats)
                .Concat(transactions.UnchangedCustomFormats)
                .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id)),
            transactions.DeletedCustomFormats.Select(m => m.ServiceId),
            serviceCfs.Select(cf => cf.Id)
        );
    }

    private record TestCacheSyncSource(
        IEnumerable<TrashIdMapping> SyncedMappings,
        IEnumerable<int> DeletedIds,
        IEnumerable<int> ValidServiceIds
    ) : ISyncStateSource;
}
