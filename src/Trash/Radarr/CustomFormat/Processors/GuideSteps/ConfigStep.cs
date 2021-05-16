using System;
using System.Collections.Generic;
using System.Linq;
using Trash.Extensions;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public class ConfigStep : IConfigStep
    {
        public List<string> CustomFormatsNotInGuide { get; } = new();
        public List<ProcessedConfigData> ConfigData { get; } = new();

        public void Process(IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
            IEnumerable<CustomFormatConfig> config)
        {
            foreach (var configCf in config)
            {
                // Also get the list of CFs that are in the guide
                var cfsInGuide = configCf.Names
                    .ToLookup(n =>
                    {
                        // Iterate up to two times:
                        // 1. Find a match in the cache using name in config. If not found,
                        // 2. Find a match in the guide using name in config.
                        return processedCfs.FirstOrDefault(
                                   cf => cf.CacheEntry?.CustomFormatName.EqualsIgnoreCase(n) ?? false) ??
                               processedCfs.FirstOrDefault(
                                   cf => cf.Name.EqualsIgnoreCase(n));
                    });

                // Names grouped under 'null' were not found in the guide OR the cache
                CustomFormatsNotInGuide.AddRange(
                    cfsInGuide[null].Distinct(StringComparer.CurrentCultureIgnoreCase));

                ConfigData.Add(new ProcessedConfigData
                {
                    CustomFormats = cfsInGuide.Where(grp => grp.Key != null).Select(grp => grp.Key!).ToList(),
                    QualityProfiles = configCf.QualityProfiles
                });
            }
        }
    }
}
