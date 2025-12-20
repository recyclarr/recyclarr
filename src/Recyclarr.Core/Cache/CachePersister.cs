using System.Text.Json;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.Cache;

public abstract class CachePersister<TCacheObject, TCache>(
    ILogger log,
    ICacheStoragePath storagePath
) : ICachePersister<TCache>
    where TCacheObject : CacheObject, new()
    where TCache : BaseCache
{
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;

    public TCache Load()
    {
        var cacheData = LoadFromJson();
        if (cacheData == null)
        {
            log.Debug("{CacheName} does not exist; proceeding without it", CacheName);
            cacheData = new TCacheObject();
        }

        return CreateCache(cacheData);
    }

    private TCacheObject? LoadFromJson()
    {
        var path = storagePath.CalculatePath<TCacheObject>();
        log.Debug("Loading {CacheName} from path: {Path}", CacheName, path.FullName);
        if (!path.Exists)
        {
            log.Debug("Cache path does not exist");
            return null;
        }

        try
        {
            using var stream = path.OpenRead();
            return JsonSerializer.Deserialize<TCacheObject>(stream, _jsonSettings);
        }
        catch (JsonException e)
        {
            log.Error(e, "Failed to read cache data, will proceed without cache");
        }

        return null;
    }

    public void Save(TCache cache)
    {
        var path = storagePath.CalculatePath<TCacheObject>();
        log.Debug("Saving {CacheName} to path {Path}", CacheName, path);

        path.CreateParentDirectory();

        using var stream = path.Create();
        JsonSerializer.Serialize(stream, cache.CacheObject, typeof(TCacheObject), _jsonSettings);
    }

    protected abstract string CacheName { get; }
    protected abstract TCache CreateCache(TCacheObject cacheObject);
}
