using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.SyncState;

/// <summary>
/// Store that maps TRaSH Guides trash_ids to Sonarr/Radarr service IDs.
/// Provides shared logic for finding, updating, and cleaning up mappings.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1002:Do not expose generic lists",
    Justification = "Mutable for Update()"
)]
public class TrashIdMappingStore(List<TrashIdMapping> mappings) : IMappingStoreView
{
    public List<TrashIdMapping> Mappings { get; } = mappings;

    IReadOnlyList<TrashIdMapping> IMappingStoreView.Mappings => Mappings;

    public int? FindId(MappingKey key)
    {
        return Mappings
            .Find(m =>
                string.Equals(m.TrashId, key.TrashId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.Name, key.Name, StringComparison.OrdinalIgnoreCase)
            )
            ?.ServiceId;
    }

    [SuppressMessage(
        "ReSharper",
        "UnusedParameter.Local",
        Justification = "l/r variables are documentation"
    )]
    public void Update(ISyncStateSource source)
    {
        var validIds = source.ValidServiceIds.ToHashSet();
        var deleted = source.DeletedIds.ToHashSet();

        // Filter to valid entries: must exist in service, not be zero, and not be deleted
        var existingMappings = Mappings
            .Where(m => m.ServiceId != 0 && validIds.Contains(m.ServiceId))
            .Where(m => !deleted.Contains(m.ServiceId))
            .DistinctBy(m => m.ServiceId);

        var result = existingMappings
            .FullOuterHashJoin(
                source.SyncedMappings,
                l => l.ServiceId,
                r => r.ServiceId,
                l => l, // Keep existing service items not in user config
                r => r, // Add new mappings from user config
                (l, r) => r // Update existing mappings with new trash_id/name from user config
            )
            .Where(m => m.ServiceId != 0)
            .OrderBy(m => m.ServiceId)
            .ToList();

        Mappings.Clear();
        Mappings.AddRange(result);
    }
}
