using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Extensions;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    internal class CustomFormatStep : ICustomFormatStep
    {
        public List<(string, string)> CustomFormatsWithOutdatedNames { get; } = new();
        public List<ProcessedCustomFormatData> ProcessedCustomFormats { get; } = new();
        public List<TrashIdMapping> DeletedCustomFormatsInCache { get; } = new();

        public Dictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; private set; } =
            new();

        public void Process(IEnumerable<string> customFormatGuideData,
            IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache)
        {
            var processedCfs = customFormatGuideData
                .Select(cf => ProcessCustomFormatData(cf, cache))
                .ToList();

            // For each ID listed under the `trash_ids` YML property, match it to an existing CF
            ProcessedCustomFormats.AddRange(config
                .SelectMany(c => c.TrashIds)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Join(processedCfs,
                    id => id,
                    cf => cf.TrashId,
                    (_, cf) => cf,
                    StringComparer.InvariantCultureIgnoreCase));

            // Build a list of CF names under the `names` property in YAML. Exclude any names that
            // are already provided by the `trash_ids` property.
            var allConfigCfNames = config
                .SelectMany(c => c.Names)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Where(n => !ProcessedCustomFormats.Any(cf => cf.CacheAwareName.EqualsIgnoreCase(n)))
                .ToList();

            // Perform updates and deletions based on matches in the cache. Matches in the cache are by ID.
            foreach (var cf in processedCfs)
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
                // removed it from their config. This will get marked for deletion later.
            }

            // Orphaned entries in cache represent custom formats we need to delete.
            ProcessDeletedCustomFormats(cache);

            // Check for multiple custom formats with the same name in the guide data (e.g. "DoVi")
            ProcessDuplicates();
        }

        private void ProcessDuplicates()
        {
            DuplicatedCustomFormats = ProcessedCustomFormats
                .GroupBy(cf => cf.Name)
                .Where(grp => grp.Count() > 1)
                .ToDictionary(grp => grp.Key, grp => grp.ToList());

            ProcessedCustomFormats.RemoveAll(cf => DuplicatedCustomFormats.ContainsKey(cf.Name));
        }

        private static ProcessedCustomFormatData ProcessCustomFormatData(string guideData, CustomFormatCache? cache)
        {
            JObject obj = JObject.Parse(guideData);
            var name = obj.ValueOrThrow<string>("name");
            var trashId = obj.ValueOrThrow<string>("trash_id");
            int? finalScore = null;

            if (obj.TryGetValue("trash_score", out var score))
            {
                finalScore = (int) score;
                obj.Property("trash_score")?.Remove();
            }

            // Remove trash_id, it's metadata that is not meant for Radarr itself
            // Radarr supposedly drops this anyway, but I prefer it to be removed by TrashUpdater
            obj.Property("trash_id")?.Remove();

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
