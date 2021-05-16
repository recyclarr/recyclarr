using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;
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
            foreach (var singleConfig in config)
            {
                var validCfs = new List<ProcessedCustomFormatData>();

                foreach (var name in singleConfig.Names)
                {
                    var match = FindCustomFormatByName(processedCfs, name);
                    if (match == null)
                    {
                        CustomFormatsNotInGuide.Add(name);
                    }
                    else
                    {
                        validCfs.Add(match);
                    }
                }

                foreach (var trashId in singleConfig.TrashIds)
                {
                    var match = processedCfs.FirstOrDefault(cf => cf.TrashId.EqualsIgnoreCase(trashId));
                    if (match == null)
                    {
                        CustomFormatsNotInGuide.Add(trashId);
                    }
                    else
                    {
                        validCfs.Add(match);
                    }
                }

                ConfigData.Add(new ProcessedConfigData
                {
                    QualityProfiles = singleConfig.QualityProfiles,
                    CustomFormats = validCfs
                        .DistinctBy(cf => cf.TrashId, StringComparer.InvariantCultureIgnoreCase)
                        .ToList()
                });
            }
        }

        private static ProcessedCustomFormatData? FindCustomFormatByName(
            IReadOnlyCollection<ProcessedCustomFormatData> processedCfs, string name)
        {
            return processedCfs.FirstOrDefault(
                       cf => cf.CacheEntry?.CustomFormatName.EqualsIgnoreCase(name) ?? false) ??
                   processedCfs.FirstOrDefault(
                       cf => cf.Name.EqualsIgnoreCase(name));
        }
    }
}
