using System.Text.Json.Serialization;

namespace Recyclarr.SyncState;

/// <summary>
/// Maps a TRaSH Guides trash_id to a Sonarr/Radarr service object ID.
/// Used to track which guide resources have been synced to which service objects.
/// </summary>
public record TrashIdMapping(string TrashId, string Name, int ServiceId)
{
    [JsonIgnore]
    public MappingKey MappingKey => new(TrashId, Name);

    // Legacy field aliases for v7.5.x cache files. Keep through v8 since the field rename was never
    // released - users upgrading from v7.5.x need these to read existing cache. Remove in v9.
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
