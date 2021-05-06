using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Trash.Extensions;
using Trash.Radarr.CustomFormat.Guide;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public class CustomFormatStep : ICustomFormatStep
    {
        public List<(string, string)> CustomFormatsWithOutdatedNames { get; } = new();
        public List<ProcessedCustomFormatData> ProcessedCustomFormats { get; } = new();
        public List<TrashIdMapping> DeletedCustomFormatsInCache { get; } = new();

        public void Process(IEnumerable<CustomFormatData> customFormatGuideData, IEnumerable<CustomFormatConfig> config,
            CustomFormatCache? cache)
        {
            var allConfigCfNames = config
                .SelectMany(c => c.Names)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var processedCfs = customFormatGuideData
                .Select(cf => ProcessCustomFormatData(cf, cache))
                .ToList();

            // Perform updates and deletions based on matches in the cache. Matches in the cache are by ID.
            foreach (var cf in processedCfs) //.Where(cf => cf.CacheEntry != null))
            {
                // Does the name of the CF in the guide match a name in the config? If yes, we keep it.
                var configName = allConfigCfNames.FirstOrDefault(n => n.EqualsIgnoreCase(cf.Name));
                if (configName != null)
                {
                    if (cf.CacheEntry != null)
                    {
                        // The cache entry might be using an old name. This will happen if:
                        // - A user has synced this CF before, AND
                        // - The name of the CF in the guide changed, AND
                        // - The user updated the name in their config to match the name in the guide.
                        cf.CacheEntry.CustomFormatName = cf.Name;
                    }

                    ProcessedCustomFormats.Add(cf);
                    continue;
                }

                // Does the name of the CF in the cache match a name in the config? If yes, we keep it.
                configName = allConfigCfNames.FirstOrDefault(n => n.EqualsIgnoreCase(cf.CacheEntry?.CustomFormatName));
                if (configName != null)
                {
                    // Config name is out of sync with the guide and should be updated
                    CustomFormatsWithOutdatedNames.Add((configName, cf.Name));
                    ProcessedCustomFormats.Add(cf);
                }

                // If we get here, we can't find a match in the config using cache or guide name, so the user must have
                // removed it from their config. This will get marked for deletion when we process those later in
                // ProcessDeletedCustomFormats().
            }

            // Orphaned entries in cache represent custom formats we need to delete.
            ProcessDeletedCustomFormats(cache);
        }

        private static ProcessedCustomFormatData ProcessCustomFormatData(CustomFormatData guideData,
            CustomFormatCache? cache)
        {
            JObject obj = JObject.Parse(guideData.Json);
            var name = obj["name"].Value<string>();
            var trashId = obj["trash_id"].Value<string>();

            // Remove trash_id, it's metadata that is not meant for Radarr itself
            // Radarr supposedly drops this anyway, but I prefer it to be removed by TrashUpdater
            obj.Property("trash_id").Remove();

            return new ProcessedCustomFormatData(name, trashId, obj)
            {
                Score = guideData.Score,
                CacheEntry = cache?.TrashIdMappings.FirstOrDefault(c => c.TrashId == trashId)
            };
        }

        private void ProcessDeletedCustomFormats(CustomFormatCache? cache)
        {
            if (cache == null)
            {
                return;
            }

            static bool MatchCfInCache(ProcessedCustomFormatData cf, TrashIdMapping c)
                => cf.CacheEntry != null && cf.CacheEntry.TrashId == c.TrashId;

            // Delete if CF is in cache and not in the guide or config
            DeletedCustomFormatsInCache.AddRange(cache.TrashIdMappings
                .Where(c => !ProcessedCustomFormats.Any(cf => MatchCfInCache(cf, c))));
        }
    }
}
