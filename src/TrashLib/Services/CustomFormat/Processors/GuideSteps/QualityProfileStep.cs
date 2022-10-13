using Common.Extensions;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

internal class QualityProfileStep : IQualityProfileStep
{
    private readonly Dictionary<string, QualityProfileCustomFormatScoreMapping> _profileScores = new();
    private readonly List<(string name, string trashId, string profileName)> _customFormatsWithoutScore = new();
    private readonly Dictionary<string, Dictionary<string, HashSet<int>>> _duplicateScores = new();

    public IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores => _profileScores;

    public IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore
        => _customFormatsWithoutScore;

    public IReadOnlyDictionary<string, Dictionary<string, HashSet<int>>> DuplicateScores => _duplicateScores;

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
                    _customFormatsWithoutScore.Add((cf.Name, cf.TrashId, profile.Name));
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

            // Check if this score was specified multiple times for the same profile. For each duplicate, we record
            // the score of the second and onward occurrences for logging/reporting purposes.
            var dupe = mapping.Mapping.FirstOrDefault(x => x.CustomFormat.TrashId.EqualsIgnoreCase(cf.TrashId));
            if (dupe is not null)
            {
                _duplicateScores.GetOrCreate(profile.Name).GetOrCreate(cf.TrashId).Add(scoreToUse.Value);
                continue;
            }

            mapping.Mapping.Add(new FormatMappingEntry(cf, scoreToUse.Value));
        }
    }
}
