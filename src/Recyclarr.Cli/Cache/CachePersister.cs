using System.Text.Json;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Cache;

public class CachePersister(ILogger log, IServiceCache cache) : ICachePersister
{
    public CustomFormatCache Load(IServiceConfiguration config)
    {
        var cache1 = cache.Load<CustomFormatCache>(config);
        if (cache1 == null)
        {
            log.Debug("Custom format cache does not exist; proceeding without it");
            return new CustomFormatCache();
        }

        // If the version is higher OR lower, we invalidate the cache. It means there's an
        // incompatibility that we do not support.
        if (cache1.Version != CustomFormatCache.LatestVersion)
        {
            log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
                cache1.Version, CustomFormatCache.LatestVersion);
            throw new CacheException("Version mismatch");
        }

        return cache1;
    }

    public void Save(IServiceConfiguration config, CustomFormatCache cache1)
    {
        log.Debug("Saving Cache with {Mappings}", JsonSerializer.Serialize(cache1.TrashIdMappings));

        cache.Save(cache1, config);
    }
}
