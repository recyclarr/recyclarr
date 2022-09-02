using TrashLib.Config.Services;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

public interface IConfigStep
{
    IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
    IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }

    void Process(IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
        IReadOnlyCollection<CustomFormatConfig> config);
}
