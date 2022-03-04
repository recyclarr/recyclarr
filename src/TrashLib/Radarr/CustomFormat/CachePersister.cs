using Common.Extensions;
using Serilog;
using TrashLib.Cache;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat;

internal class CachePersister : ICachePersister
{
    private readonly IServiceCache _cache;

    public CachePersister(ILogger log, IServiceCache cache)
    {
        Log = log;
        _cache = cache;
    }

    private ILogger Log { get; }
    public CustomFormatCache? CfCache { get; private set; }

    public void Load()
    {
        CfCache = _cache.Load<CustomFormatCache>();
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (CfCache != null)
        {
            Log.Debug("Loaded Cache");

            // If the version is higher OR lower, we invalidate the cache. It means there's an
            // incompatibility that we do not support.
            if (CfCache.Version != CustomFormatCache.LatestVersion)
            {
                Log.Information("Cache version mismatch ({OldVersion} vs {LatestVersion}); ignoring cache data",
                    CfCache.Version, CustomFormatCache.LatestVersion);
                CfCache = null;
            }
        }
        else
        {
            Log.Debug("Custom format cache does not exist; proceeding without it");
        }
    }

    public void Save()
    {
        if (CfCache == null)
        {
            Log.Debug("Not saving cache because it is null");
            return;
        }

        Log.Debug("Saving Cache");
        _cache.Save(CfCache);
    }

    public void Update(IEnumerable<ProcessedCustomFormatData> customFormats)
    {
        Log.Debug("Updating cache");
        CfCache = new CustomFormatCache();
        CfCache!.TrashIdMappings.AddRange(customFormats
            .Where(cf => cf.CacheEntry != null)
            .Select(cf => cf.CacheEntry!));
    }
}
