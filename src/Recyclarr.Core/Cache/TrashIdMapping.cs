namespace Recyclarr.Cache;

/// <summary>
/// Maps a TRaSH Guides trash_id to a Sonarr/Radarr service object ID.
/// Used to track which guide resources have been synced to which service objects.
/// </summary>
public record TrashIdMapping(string TrashId, string Name, int ServiceId);
