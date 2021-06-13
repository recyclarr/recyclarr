using System.Collections.Generic;
using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.CustomFormat.Models
{
    public class ProcessedConfigData
    {
        public List<ProcessedCustomFormatData> CustomFormats { get; init; } = new();
        public List<QualityProfileConfig> QualityProfiles { get; init; } = new();
    }
}
