using System.Collections.ObjectModel;
using Recyclarr.TrashLib.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

[CacheObjectName("custom-format-cache")]
public record CustomFormatCache
{
    public const int LatestVersion = 1;

    public int Version { get; init; } = LatestVersion;
    public Collection<TrashIdMapping> TrashIdMappings { get; init; } = new();
}

public record TrashIdMapping(string TrashId, string CustomFormatName, int CustomFormatId);
