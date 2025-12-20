using System.Text.Json.Serialization;

namespace Recyclarr.Cache;

/// <summary>
/// Maps a TRaSH Guides trash_id to a Sonarr/Radarr service object ID.
/// Used to track which guide resources have been synced to which service objects.
/// </summary>
public record TrashIdMapping(string TrashId, string Name, int ServiceId)
{
    // Legacy field aliases (v7.5 and earlier) - remove on next major version
    [JsonInclude]
    internal string? CustomFormatName
    {
        init => Name = value ?? "";
    }

    [JsonInclude]
    internal int? CustomFormatId
    {
        init => ServiceId = value ?? 0;
    }
}
