using Recyclarr.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

internal class CustomFormatCachePersister(
    ILogger log,
    ICacheStoragePath storagePath,
    IServiceConfiguration config
) : CachePersister<CustomFormatCacheObject, CustomFormatCache>(log, storagePath, config)
{
    protected override string CacheName => "Custom Format Cache";

    protected override CustomFormatCache CreateCache(CustomFormatCacheObject cacheObject)
    {
        return new CustomFormatCache(cacheObject);
    }
}
