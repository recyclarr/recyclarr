using Newtonsoft.Json;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Cache;

public class CachePersister : ICachePersister
{
    private readonly IServiceCache _cache;
    private readonly ILogger _log;

    public CachePersister(ILogger log, IServiceCache cache)
    {
        _log = log;
        _cache = cache;
    }

    public CustomFormatCache Load(IServiceConfiguration config)
    {
        var cache = _cache.Load<CustomFormatCache>(config);
        if (cache == null)
        {
            _log.Debug("Custom format cache does not exist; proceeding without it");
            return new CustomFormatCache();
        }

        // If the version is higher OR lower, we invalidate the cache. It means there's an
        // incompatibility that we do not support.
        if (cache.Version != CustomFormatCache.LatestVersion)
        {
            _log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
                cache.Version, CustomFormatCache.LatestVersion);
            throw new CacheException("Version mismatch");
        }

        return cache;
    }

    public void Save(IServiceConfiguration config, CustomFormatCache cache)
    {
        _log.Debug("Saving Cache with {Mappings}", JsonConvert.SerializeObject(cache.TrashIdMappings));
        _cache.Save(cache, config);
    }
}
