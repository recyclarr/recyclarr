using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors;

internal interface IGuideProcessor
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
    IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }
    IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
    IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
    IReadOnlyDictionary<string, Dictionary<string, HashSet<int>>> DuplicateScores { get; }

    Task BuildGuideDataAsync(
        IEnumerable<CustomFormatConfig> config,
        CustomFormatCache? cache,
        SupportedServices serviceType);
}
