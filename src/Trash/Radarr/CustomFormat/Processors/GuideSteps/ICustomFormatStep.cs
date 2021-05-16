using System.Collections.Generic;
using Trash.Radarr.CustomFormat.Guide;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface ICustomFormatStep
    {
        List<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
        List<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        List<(string, string)> CustomFormatsWithOutdatedNames { get; }
        Dictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

        void Process(IEnumerable<CustomFormatData> customFormatGuideData,
            IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
    }
}
