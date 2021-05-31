using System.Collections.Generic;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.GuideSteps
{
    public class QualityProfileStep : IQualityProfileStep
    {
        public Dictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; } = new();
        public List<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; } = new();

        public void Process(IEnumerable<ProcessedConfigData> configData)
        {
            foreach (var config in configData)
            foreach (var profile in config.QualityProfiles)
            foreach (var cf in config.CustomFormats)
            {
                // Check if there is a score we can use. Priority is:
                //      1. Score from the YAML config is used. If user did not provide,
                //      2. Score from the guide is used. If the guide did not have one,
                //      3. Warn the user and
                var scoreToUse = profile.Score;
                if (scoreToUse == null)
                {
                    if (cf.Score == null)
                    {
                        CustomFormatsWithoutScore.Add((cf.Name, cf.TrashId, profile.Name));
                    }
                    else
                    {
                        scoreToUse = cf.Score.Value;
                    }
                }

                if (scoreToUse == null)
                {
                    continue;
                }

                if (!ProfileScores.TryGetValue(profile.Name, out var mapping))
                {
                    mapping = new QualityProfileCustomFormatScoreMapping(profile.ResetUnmatchedScores);
                    ProfileScores[profile.Name] = mapping;
                }

                mapping.Mapping.Add(new FormatMappingEntry(cf, scoreToUse.Value));
            }
        }
    }
}
