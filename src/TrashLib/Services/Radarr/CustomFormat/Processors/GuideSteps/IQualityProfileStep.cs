using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.GuideSteps;

public interface IQualityProfileStep
{
    IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
    IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
    void Process(IEnumerable<ProcessedConfigData> configData);
}
