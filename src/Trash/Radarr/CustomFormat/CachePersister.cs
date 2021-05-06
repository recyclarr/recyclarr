using System.Collections.Generic;
using System.Linq;
using Serilog;
using Trash.Cache;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat
{
    public class CachePersister : ICachePersister
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
}
