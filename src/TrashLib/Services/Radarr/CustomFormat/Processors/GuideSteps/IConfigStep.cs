using TrashLib.Services.Radarr.Config;
using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.GuideSteps;

public interface IConfigStep
{
    IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
    IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }

    void Process(IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
        IReadOnlyCollection<CustomFormatConfig> config);
}
