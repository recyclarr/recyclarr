using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

public class ProcessedConfigData
{
    public ICollection<CustomFormatData> CustomFormats { get; init; }
        = new List<CustomFormatData>();

    public ICollection<AssignScoresToConfig> QualityProfiles { get; init; }
        = new List<AssignScoresToConfig>();
}
