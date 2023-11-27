using System.Text.Json;
using Recyclarr.Cli.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public class CustomFormatCachePersister(ILogger log, IServiceCache serviceCache) : ICustomFormatCachePersister
{
    public const int LatestVersion = 1;

    public CustomFormatCache Load(IServiceConfiguration config)
    {
        var cacheData = serviceCache.Load<CustomFormatCacheData>(config);
        if (cacheData == null)
        {
            log.Debug("Custom format cache does not exist; proceeding without it");
            cacheData = new CustomFormatCacheData(LatestVersion, config.InstanceName, []);
        }

        // If the version is higher OR lower, we invalidate the cache. It means there's an
        // incompatibility that we do not support.
        if (cacheData.Version != LatestVersion)
        {
            log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
                cacheData.Version, LatestVersion);
            throw new CacheException("Version mismatch");
        }

        return new CustomFormatCache(cacheData.TrashIdMappings);
    }

    public void Save(IServiceConfiguration config, CustomFormatCache cache)
    {
        var data = new CustomFormatCacheData(LatestVersion, config.InstanceName, cache.Mappings);
        log.Debug("Saving Custom Format Cache with {Mappings}", JsonSerializer.Serialize(data.TrashIdMappings));
        serviceCache.Save(data, config);
    }
}
