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

    protected void RemoveStale(IEnumerable<int> validServiceIds)
    {
        var validIds = validServiceIds.ToHashSet();

        cacheObject.Mappings.RemoveAll(m => m.ServiceId == 0 || !validIds.Contains(m.ServiceId));

        // Clean up duplicate IDs - keep first occurrence, remove the rest.
        // Duplicates can occur from edge cases and corrupt subsequent transaction processing.
        var duplicatesToRemove = cacheObject
            .Mappings.GroupBy(m => m.ServiceId)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        cacheObject.Mappings.RemoveAll(duplicatesToRemove.Contains);
    }

    [SuppressMessage(
        "ReSharper",
        "UnusedParameter.Local",
        Justification = "LINQ l/r variables double as documentation"
    )]
    protected void Update(IEnumerable<TrashIdMapping> syncedMappings, IEnumerable<int> deletedIds)
    {
        // Assumes RemoveStale() was called first, so Mappings contains only valid existing entries.
        var deleted = deletedIds.ToHashSet();

        var result = cacheObject
            .Mappings.DistinctBy(m => m.ServiceId)
            .Where(m => !deleted.Contains(m.ServiceId))
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
