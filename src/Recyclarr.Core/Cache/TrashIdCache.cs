using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Cache;

/// <summary>
/// Generic cache that maps TRaSH Guides trash_ids to Sonarr/Radarr service IDs.
/// Provides shared logic for finding, updating, and cleaning up mappings.
/// </summary>
public class TrashIdCache<TCacheObject>(TCacheObject cacheObject) : BaseCache(cacheObject)
    where TCacheObject : CacheObject, ITrashIdCacheObject
{
    public IReadOnlyList<TrashIdMapping> Mappings => cacheObject.Mappings;

    public int? FindId(string trashId)
    {
        return cacheObject.Mappings.Find(m => m.TrashId == trashId)?.ServiceId;
    }

    public void Update(ICacheSyncSource source)
    {
        Update(source.SyncedMappings, source.DeletedIds, source.ValidServiceIds);
    }

    [SuppressMessage(
        "ReSharper",
        "UnusedParameter.Local",
        Justification = "LINQ l/r variables double as documentation"
    )]
    private void Update(
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
