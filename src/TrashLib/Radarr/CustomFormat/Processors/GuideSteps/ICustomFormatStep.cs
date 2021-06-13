using System.Collections.Generic;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface ICustomFormatStep
    {
        List<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
        List<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        List<(string, string)> CustomFormatsWithOutdatedNames { get; }
        Dictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

        void Process(IEnumerable<string> customFormatGuideData,
            IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
    }
}
