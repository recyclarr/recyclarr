namespace Recyclarr.SyncState;

/// <summary>
/// Maps a TRaSH Guides trash_id to a Sonarr/Radarr service object ID.
/// Used to track which guide resources have been synced to which service objects.
/// </summary>
public record TrashIdMapping
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }
    public required int ServiceId { get; init; }
}
