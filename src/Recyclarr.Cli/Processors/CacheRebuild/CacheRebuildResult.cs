using Recyclarr.Cache;

namespace Recyclarr.Cli.Processors.CacheRebuild;

internal record CacheRebuildResult(
    List<TrashIdMapping> Mappings,
    CacheRebuildStats Stats,
    List<CfCacheDetail> Details
);
