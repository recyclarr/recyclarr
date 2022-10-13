using TrashLib.Config.Services;
using TrashLib.Services.Common;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Processors;

internal interface IGuideProcessor
{
    IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
    IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
    IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }
    IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
    IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
    IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
    IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames { get; }
    IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }
    IReadOnlyDictionary<string, Dictionary<string, HashSet<int>>> DuplicateScores { get; }

    Task BuildGuideDataAsync(IEnumerable<CustomFormatConfig> config, CustomFormatCache? cache,
        IGuideService guideService);

    void Reset();
}
