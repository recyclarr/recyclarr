using TrashLib.Config.Services;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

public interface ICustomFormatStep
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }

    void Process(IList<CustomFormatData> customFormatGuideData,
        IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
}
