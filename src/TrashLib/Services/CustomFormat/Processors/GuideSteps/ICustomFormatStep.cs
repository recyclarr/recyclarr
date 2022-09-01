using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;
using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

public interface ICustomFormatStep
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
    IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames { get; }
    IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

    void Process(IList<CustomFormatData> customFormatGuideData,
        IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
}
