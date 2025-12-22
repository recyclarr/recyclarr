using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

internal class CustomFormatCachePersister(ILogger log, ICacheStoragePath storagePath)
    : CachePersister<CustomFormatCacheObject>(log, storagePath)
{
    protected override string CacheName => "Custom Format Cache";
}
