using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

/// <summary>
/// Custom format-specific cache that adapts CF types to the generic TrashIdCache.
/// </summary>
internal class CustomFormatCache(CustomFormatCacheObject cacheObject)
    : TrashIdCache<CustomFormatCacheObject>(cacheObject)
{
    public int? FindId(CustomFormatResource cf)
    {
        return FindId(cf.TrashId);
    }

    public void Update(
        CustomFormatTransactionData transactions,
        IEnumerable<CustomFormatResource> serviceCfs
    )
    {
        var syncedMappings = transactions
            .UpdatedCustomFormats.Concat(transactions.UnchangedCustomFormats)
            .Concat(transactions.NewCustomFormats)
            .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id));

        var deletedIds = transactions.DeletedCustomFormats.Select(cf => cf.ServiceId);
        var validServiceIds = serviceCfs.Select(cf => cf.Id);

        Update(syncedMappings, deletedIds, validServiceIds);
    }
}
