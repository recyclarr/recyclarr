using System.Collections.Generic;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface ICustomFormatProcessor
    {
        List<ProcessedCustomFormatData> CustomFormats { get; }
        List<TrashIdMapping> DeletedCustomFormatsInCache { get; }

        void Process(IEnumerable<string> customFormatGuideData, RadarrConfig config);
    }
}
