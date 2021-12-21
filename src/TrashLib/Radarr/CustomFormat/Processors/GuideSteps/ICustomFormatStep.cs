using System.Collections.Generic;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps;

public interface ICustomFormatStep
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
    IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames { get; }
    IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

    void Process(IEnumerable<string> customFormatGuideData,
        IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
}
