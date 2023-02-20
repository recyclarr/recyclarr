using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

public class ProcessedConfigData
{
    public ICollection<CustomFormatData> CustomFormats { get; init; }
        = new List<CustomFormatData>();

    public ICollection<QualityProfileScoreConfig> QualityProfiles { get; init; }
        = new List<QualityProfileScoreConfig>();
}
