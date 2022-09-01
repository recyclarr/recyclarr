using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.CustomFormat.Models;

public class ProcessedConfigData
{
    public ICollection<ProcessedCustomFormatData> CustomFormats { get; init; }
        = new List<ProcessedCustomFormatData>();

    public ICollection<QualityProfileConfig> QualityProfiles { get; init; }
        = new List<QualityProfileConfig>();
}
