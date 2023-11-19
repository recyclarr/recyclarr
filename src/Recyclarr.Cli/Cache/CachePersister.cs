using System.Text.Json;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Cache;

public class CachePersister(ILogger log, IServiceCache serviceCache) : ICachePersister
{
    public CustomFormatCache Load(IServiceConfiguration config)
    {
        var cache = serviceCache.Load<CustomFormatCache>(config);
        if (cache == null)
        {
            log.Debug("Custom format cache does not exist; proceeding without it");
            return new CustomFormatCache();
        }

        // If the version is higher OR lower, we invalidate the cache. It means there's an
        // incompatibility that we do not support.
        if (cache.Version != CustomFormatCache.LatestVersion)
        {
            log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
                cache.Version, CustomFormatCache.LatestVersion);
            throw new CacheException("Version mismatch");
        }

        return cache;
    }

    public void Save(IServiceConfiguration config, CustomFormatCache cache)
    {
        log.Debug("Saving Cache with {Mappings}", JsonSerializer.Serialize(cache.TrashIdMappings));

        serviceCache.Save(cache, config);
    }
}
