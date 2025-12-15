namespace Recyclarr.Cli.Processors.CacheRebuild;

internal record CfCacheDetail(
    string Name,
    string TrashId,
    int? ServiceId,
    int? CachedServiceId,
    CfCacheState State
);
