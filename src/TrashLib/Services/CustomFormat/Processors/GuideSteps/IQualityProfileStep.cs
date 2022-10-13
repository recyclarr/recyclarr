using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

public interface IQualityProfileStep
{
    IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
    IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
    IReadOnlyDictionary<string, Dictionary<string, HashSet<int>>> DuplicateScores { get; }
    void Process(IEnumerable<ProcessedConfigData> configData);
}
