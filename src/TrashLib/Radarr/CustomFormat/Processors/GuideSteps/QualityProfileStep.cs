using System.Collections.Generic;
using System.Linq;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    internal class QualityProfileStep : IQualityProfileStep
    {
        public Dictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; } = new();
        public List<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; } = new();

        public void Process(RadarrConfig config, IReadOnlyCollection<ProcessedCustomFormatData> customFormats)
        {
            // foreach (var config in configData)
            foreach (var profile in config.QualityProfiles)
            foreach (var cfScore in profile.Scores)
            {
                var matchingCf = customFormats.FirstOrDefault(cf => cf.TrashId == cfScore.TrashId);
                if (matchingCf is null)
                {
                    continue;
                }

                // Check if there is a score we can use. Priority is:
                //      1. Score from the YAML config is used. If user did not provide,
                //      2. Score from the guide is used. If the guide did not have one,
                //      3. Record the CF without a score
                var scoreToUse = matchingCf?.Score;
                if (scoreToUse is null)
                {
                    if (cfScore.Score is null)
                    {
                        var name = matchingCf!.Name;
                        CustomFormatsWithoutScore.Add((name, cfScore.TrashId, profile.ProfileName));
                    }
                    else
                    {
                        scoreToUse = cfScore.Score.Value;
                    }
                }

                if (scoreToUse is null)
                {
                    continue;
                }

                if (!ProfileScores.TryGetValue(profile.ProfileName, out var mapping))
                {
                    mapping = new QualityProfileCustomFormatScoreMapping(profile.ResetUnmatchedScores);
                    ProfileScores[profile.ProfileName] = mapping;
                }

                mapping.Mapping.Add(new FormatMappingEntry(matchingCf!, scoreToUse.Value));
            }
        }
    }
}
