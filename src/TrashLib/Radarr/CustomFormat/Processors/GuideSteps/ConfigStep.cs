using Common.Extensions;
using Serilog;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps;

public class ConfigStep : IConfigStep
{
    private readonly ILogger _log;
    private readonly List<ProcessedConfigData> _configData = new();
    private readonly List<string> _customFormatsNotInGuide = new();

    public IReadOnlyCollection<string> CustomFormatsNotInGuide => _customFormatsNotInGuide;
    public IReadOnlyCollection<ProcessedConfigData> ConfigData => _configData;

    public ConfigStep(ILogger log)
    {
        _log = log;
    }

    public void Process(
        IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
        IReadOnlyCollection<CustomFormatConfig> config)
    {
        if (config.SelectMany(x => x.Names).Any())
        {
            _log.Warning(
                "`names` list for `custom_formats` is deprecated and will be removed in the future; use " +
                "`trash_ids` instead");
        }

        foreach (var singleConfig in config)
        {
            var validCfs = new List<ProcessedCustomFormatData>();

            foreach (var name in singleConfig.Names)
            {
                var match = FindCustomFormatByName(processedCfs, name);
                if (match == null)
                {
                    _customFormatsNotInGuide.Add(name);
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
                    _customFormatsNotInGuide.Add(trashId);
                }
                else
                {
                    validCfs.Add(match);
                }
            }

            _configData.Add(new ProcessedConfigData
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
        return processedCfs.FirstOrDefault(cf => cf.CacheEntry?.CustomFormatName.EqualsIgnoreCase(name) ?? false)
               ?? processedCfs.FirstOrDefault(cf => cf.Name.EqualsIgnoreCase(name));
    }
}
