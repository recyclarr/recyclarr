using Common.Extensions;
using TrashLib.Config.Services;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

/// <remarks>
/// The purpose of this step is to validate the custom format data in the configs:
///
/// - Validate that custom formats specified in the config exist in the guide.
/// - Removal of duplicates.
/// </remarks>
public class ConfigStep : IConfigStep
{
    private readonly List<ProcessedConfigData> _configData = new();
    private readonly List<string> _customFormatsNotInGuide = new();

    public IReadOnlyCollection<string> CustomFormatsNotInGuide => _customFormatsNotInGuide;
    public IReadOnlyCollection<ProcessedConfigData> ConfigData => _configData;

    public void Process(
        IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
        IReadOnlyCollection<CustomFormatConfig> config)
    {
        foreach (var singleConfig in config)
        {
            var validCfs = new List<ProcessedCustomFormatData>();

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
}
