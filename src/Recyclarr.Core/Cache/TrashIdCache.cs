using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Cache;

/// <summary>
/// Generic base class for caches that map TRaSH Guides trash_ids to Sonarr/Radarr service IDs.
/// Provides shared logic for finding, updating, and cleaning up mappings.
/// Subclasses (CustomFormatCache, QualityProfileCache) provide type-specific adapters.
/// </summary>
public class TrashIdCache<TCacheObject>(TCacheObject cacheObject) : BaseCache(cacheObject)
    where TCacheObject : CacheObject, ITrashIdCacheObject
{
    public IReadOnlyList<TrashIdMapping> Mappings => cacheObject.Mappings;

    protected int? FindId(string trashId)
    {
        return cacheObject.Mappings.Find(m => m.TrashId == trashId)?.ServiceId;
    }

    [SuppressMessage(
        "ReSharper",
        "UnusedParameter.Local",
        Justification = "LINQ l/r variables double as documentation"
    )]
    protected void Update(
        IEnumerable<TrashIdMapping> syncedMappings,
        IEnumerable<int> deletedIds,
        IEnumerable<int> validServiceIds
    )
    {
        var validIds = validServiceIds.ToHashSet();
        var deleted = deletedIds.ToHashSet();

        // Filter to valid entries: must exist in service, not be zero, and not be deleted
        var existingMappings = cacheObject
            .Mappings.Where(m => m.ServiceId != 0 && validIds.Contains(m.ServiceId))
            .Where(m => !deleted.Contains(m.ServiceId))
            .DistinctBy(m => m.ServiceId);

        var result = existingMappings
            .FullOuterHashJoin(
                syncedMappings,
                l => l.ServiceId,
                r => r.ServiceId,
                l => l, // Keep existing service items not in user config
                r => r, // Add new mappings from user config
                (l, r) => r // Update existing mappings with new trash_id/name from user config
            )
            .Where(m => m.ServiceId != 0)
            .OrderBy(m => m.ServiceId)
            .ToList();

        cacheObject.Mappings.Clear();
        cacheObject.Mappings.AddRange(result);
    }
}
