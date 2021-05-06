using System.Collections.Generic;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public interface IQualityProfileStep
    {
        Dictionary<string, List<QualityProfileCustomFormatScoreEntry>> ProfileScores { get; }
        List<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
        void Process(IEnumerable<ProcessedConfigData> configData);
    }
}
