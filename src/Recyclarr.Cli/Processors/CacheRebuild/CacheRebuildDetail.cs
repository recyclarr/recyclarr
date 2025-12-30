namespace Recyclarr.Cli.Processors.CacheRebuild;

internal record CacheRebuildDetail(
    string Name,
    string TrashId,
    string? CachedTrashId,
    int? ServiceId,
    int? CachedServiceId,
    CacheRebuildState State
);
