using System.Collections.ObjectModel;
using Recyclarr.TrashLib.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

[CacheObjectName("custom-format-cache")]
public class CustomFormatCache
{
    public const int LatestVersion = 1;

    public int Version { get; init; } = LatestVersion;
    public Collection<TrashIdMapping> TrashIdMappings { get; init; } = new();
}

public class TrashIdMapping
{
    public TrashIdMapping(string trashId, int customFormatId = default)
    {
        TrashId = trashId;
        CustomFormatId = customFormatId;
    }

    public string TrashId { get; }
    public int CustomFormatId { get; set; }
}
