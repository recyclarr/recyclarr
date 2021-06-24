using System.Collections.Generic;
using System.Linq;
using Serilog;
using TrashLib.Cache;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    internal class CachePersister : ICachePersister
    {
        private readonly IServiceCache _cache;
        private readonly ICacheGuidBuilder _guidBuilder;

        public CachePersister(ILogger log, IServiceCache cache, ICacheGuidBuilder guidBuilder)
        {
            Log = log;
            _cache = cache;
            _guidBuilder = guidBuilder;
        }

        private ILogger Log { get; }

        public CustomFormatCache? CfCache { get; private set; }

        public void Load()
        {
            CfCache = _cache.Load<CustomFormatCache>(_guidBuilder);
            if (CfCache == null)
            {
                Log.Debug("Custom format cache does not exist; proceeding without it");
                return;
            }

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

        public void Save()
        {
            if (CfCache == null)
            {
                Log.Debug("Not saving cache because it is null");
                return;
            }

            Log.Debug("Saving Cache");
            _cache.Save(CfCache, _guidBuilder);
        }

        public void Update(IEnumerable<ProcessedCustomFormatData> customFormats)
        {
            Log.Debug("Updating cache");
            CfCache = new CustomFormatCache();
            CfCache.TrashIdMappings.AddRange(customFormats
                .Where(cf => cf.CacheEntry != null)
                .Select(cf => cf.CacheEntry!));
        }
    }
}
