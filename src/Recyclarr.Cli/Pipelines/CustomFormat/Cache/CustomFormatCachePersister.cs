using System.Text.Json;
using Recyclarr.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public abstract class CacheHandler<T> where T : CacheData
{
    private readonly ILogger _log;
    private readonly IServiceCache _cache;
    private readonly IServiceConfiguration _config;
    private readonly int _latestVersion;

    protected CacheHandler(ILogger log, IServiceCache cache, IServiceConfiguration config, int latestVersion)
    {
        _log = log;
        _cache = cache;
        _config = config;
        _latestVersion = latestVersion;
    }

    public T Load()
    {
        var cacheData = _cache.Load<T>();
        if (cacheData == null)
        {
            _log.Debug("Custom format cache does not exist; proceeding without it");
            cacheData = MakeDefaultCacheObject(_latestVersion, _config.InstanceName);
        }

        // If the version is higher OR lower, we invalidate the cache. It means there's an
        // incompatibility that we do not support.
        if (cacheData.Version != _latestVersion)
        {
            HandleVersionMismatch(cacheData);
        }

        return cacheData;
    }

    public void Save(T cacheData)
    {
        var data = new CustomFormatCacheData
        {
            Version = LatestVersion,
            InstanceName = _config1.InstanceName,
            TrashIdMappings = cacheData.Mappings
        };

        _log1.Debug("Saving Custom Format Cache with {Mappings}", JsonSerializer.Serialize(data.TrashIdMappings));
        _cache.Save(data);
    }

    protected abstract T MakeDefaultCacheObject(int latestVersion, string instanceName);

    protected virtual void HandleVersionMismatch(T cacheData)
    {
        _log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
            cacheData.Version, _latestVersion);
        throw new CacheException("Version mismatch");
    }
}

public class CustomFormatCachePersister(
    ILogger log,
    IServiceCache cache,
    IServiceConfiguration config)
    : CacheHandler<CustomFormatCacheData>(log, cache, config, latestVersion: 1), ICustomFormatCachePersister
{
    protected override CustomFormatCacheData MakeDefaultCacheObject(int latestVersion, string instanceName)
    {
        return new CustomFormatCacheData
        {
            Version = latestVersion,
            InstanceName = instanceName
        };
    }
}
