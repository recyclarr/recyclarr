using System.Collections.Generic;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface ICustomFormatProcessor
    {
        List<ProcessedCustomFormatData> CustomFormats { get; }
        void Process(IEnumerable<string> customFormatGuideData, RadarrConfig config);
    }
}
