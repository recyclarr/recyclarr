using System.Collections.Generic;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface IConfigStep
    {
        IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
        IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }

        void Process(IReadOnlyCollection<ProcessedCustomFormatData> processedCfs,
            IEnumerable<CustomFormatConfig> config);
    }
}
