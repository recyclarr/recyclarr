using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.SyncState;

/// <summary>
/// Generic store that maps TRaSH Guides trash_ids to Sonarr/Radarr service IDs.
/// Provides shared logic for finding, updating, and cleaning up mappings.
/// </summary>
public class TrashIdMappingStore<TStateObject>(TStateObject stateObject)
    : BaseMappingStore(stateObject)
    where TStateObject : SyncStateObject, ITrashIdMappings
{
    public IReadOnlyList<TrashIdMapping> Mappings => stateObject.Mappings;

    public int? FindId(string trashId)
    {
        return stateObject.Mappings.Find(m => m.TrashId == trashId)?.ServiceId;
    }

    public void Update(ISyncStateSource source)
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
        var existingMappings = stateObject
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

        stateObject.Mappings.Clear();
        stateObject.Mappings.AddRange(result);
    }
}
