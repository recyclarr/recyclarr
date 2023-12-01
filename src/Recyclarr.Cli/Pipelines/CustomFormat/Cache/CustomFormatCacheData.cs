using JetBrains.Annotations;
using Recyclarr.Cli.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public record TrashIdMapping(string TrashId, string CustomFormatName, int CustomFormatId);

[CacheObjectName("custom-format-cache")]
public record CustomFormatCacheData(
    int Version,
    [UsedImplicitly] string InstanceName,
    IReadOnlyCollection<TrashIdMapping> TrashIdMappings);
