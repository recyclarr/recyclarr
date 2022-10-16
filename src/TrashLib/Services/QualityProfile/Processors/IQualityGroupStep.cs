using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.QualityProfile.Processors;

public interface IQualityGroupStep
{
//    IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
    void Process(IEnumerable<ProcessedConfigData> configData);
}
