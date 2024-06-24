using JetBrains.Annotations;
using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public record CacheData
{
    public int Version { get; init; }
    public string InstanceName { get; init; } = "";
}

[CacheObjectName("custom-format-cache")]
public record CustomFormatCacheData : CacheData
{
    public IReadOnlyCollection<CfTrashIdMapping> TrashIdMappings { get; init; } = Array.Empty<CfTrashIdMapping>();
}
