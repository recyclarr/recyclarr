using TrashLib.Config.Services;

namespace TrashLib.Services.CustomFormat.Models;

public class ProcessedConfigData
{
    public ICollection<ProcessedCustomFormatData> CustomFormats { get; init; }
        = new List<ProcessedCustomFormatData>();

    public ICollection<QualityProfileScoreConfig> QualityProfiles { get; init; }
        = new List<QualityProfileScoreConfig>();
}
