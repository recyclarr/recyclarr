using System.Collections.Generic;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface IConfigStep
    {
        List<string> CustomFormatsNotInGuide { get; }
        List<ProcessedConfigData> ConfigData { get; }

        void Process(IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
            IEnumerable<CustomFormatConfig> config);
    }
}
