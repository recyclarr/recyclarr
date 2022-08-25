using TrashLib.Services.Radarr.Config;
using TrashLib.Services.Radarr.CustomFormat.Models;
using TrashLib.Services.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.GuideSteps;

public interface ICustomFormatStep
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
    IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames { get; }
    IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

    void Process(IList<CustomFormatData> customFormatGuideData,
        IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
}
