using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    internal class CustomFormatProcessor : ICustomFormatProcessor
    {
        public List<(string, string)> CustomFormatsWithOutdatedNames { get; } = new();
        public List<ProcessedCustomFormatData> ProcessedCustomFormats { get; } = new();
        public List<TrashIdMapping> DeletedCustomFormatsInCache { get; } = new();

        public void Process(IEnumerable<string> customFormatGuideData, RadarrConfig config, CustomFormatCache? cache)
        {
            var processedCfs = customFormatGuideData
                .Select(jsonData => ProcessCustomFormatData(jsonData, cache))
                .ToList();

            // For each ID listed under the `trash_ids` YML property, match it to an existing CF
            ProcessedCustomFormats.AddRange(config.CustomFormats
                .Select(c => c.TrashId)
                .Distinct()
                .Join(processedCfs,
                    id => id,
                    cf => cf.TrashId,
                    (_, cf) => cf,
                    StringComparer.InvariantCultureIgnoreCase));

            // Orphaned entries in cache represent custom formats we need to delete.
            ProcessDeletedCustomFormats(cache);
        }

        private static ProcessedCustomFormatData ProcessCustomFormatData(string guideData, CustomFormatCache? cache)
        {
            JObject obj = JObject.Parse(guideData);
            var name = (string) obj["name"];
            var trashId = (string) obj["trash_id"];
            int? finalScore = null;

            if (obj.TryGetValue("trash_score", out var score))
            {
                finalScore = (int) score;
                obj.Property("trash_score").Remove();
            }

            // Remove trash_id, it's metadata that is not meant for Radarr itself
            // Radarr supposedly drops this anyway, but I prefer it to be removed by TrashUpdater
            obj.Property("trash_id").Remove();

            return new ProcessedCustomFormatData(name, trashId, obj)
            {
                Score = finalScore,
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
