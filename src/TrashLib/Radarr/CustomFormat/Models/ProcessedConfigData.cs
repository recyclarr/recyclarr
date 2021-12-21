using System.Collections.Generic;
using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.CustomFormat.Models;

public class ProcessedConfigData
{
    public ICollection<ProcessedCustomFormatData> CustomFormats { get; init; }
        = new List<ProcessedCustomFormatData>();

    public ICollection<QualityProfileConfig> QualityProfiles { get; init; }
        = new List<QualityProfileConfig>();
}
