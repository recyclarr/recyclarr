using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class CfCache
{
    public static TrashIdCache<CustomFormatCacheObject> New(params TrashIdMapping[] mappings)
    {
        return new TrashIdCache<CustomFormatCacheObject>(
            new CustomFormatCacheObject { Mappings = mappings.ToList() }
        );
    }

    public static ICacheSyncSource ToSyncSource(
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
    ) : ICacheSyncSource;
}
