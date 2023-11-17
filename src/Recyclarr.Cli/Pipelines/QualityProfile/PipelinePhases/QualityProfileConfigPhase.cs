using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record ProcessedQualityProfileScore(string TrashId, string CfName, int FormatId, int Score);

public record ProcessedQualityProfileData
{
    public required QualityProfileConfig Profile { get; init; }
    public bool ShouldCreate { get; init; } = true;
    public IList<ProcessedQualityProfileScore> CfScores { get; init; } = new List<ProcessedQualityProfileScore>();
    public IList<CustomFormatData> ScorelessCfs { get; } = new List<CustomFormatData>();
}

public class QualityProfileConfigPhase(ILogger log, ProcessedCustomFormatCache cache)
{
    public IReadOnlyCollection<ProcessedQualityProfileData> Execute(IServiceConfiguration config)
    {
        // 1. For each group of CFs that has a quality profile specified
        // 2. For each quality profile score config in that CF group
        // 3. For each CF in the group above, match it to a Guide CF object and pair it with the quality profile config
        var profileAndCfs = config.CustomFormats
            .SelectMany(x => x.QualityProfiles
                .Select(y => (Profile: y, x.TrashIds)))
            .SelectMany(x => x.TrashIds
                .Select(cache.LookupByTrashId)
                .NotNull()
                .Select(y => (x.Profile, Cf: y)));

        var allProfiles = config.QualityProfiles
            .Select(x => new ProcessedQualityProfileData {Profile = x})
            .ToDictionary(x => x.Profile.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

        foreach (var (profile, cf) in profileAndCfs)
        {
            if (!allProfiles.TryGetValue(profile.Name, out var profileCfs))
            {
                log.Debug("Implicitly adding quality profile config for {ProfileName}", profile.Name);

                // If the user did not specify a quality profile in their config, we still create the QP object
                // for consistency (at the very least for the name).
                allProfiles[profile.Name] = profileCfs =
                    new ProcessedQualityProfileData
                    {
                        Profile = new QualityProfileConfig {Name = profile.Name},
                        // The user must explicitly specify a profile in the top-level `quality_profiles` section of
                        // their config, otherwise we do not implicitly create them in the service.
                        ShouldCreate = false
                    };
            }

            AddCustomFormatScoreData(profileCfs, profile, cf);
        }

        var profilesToReturn = allProfiles.Values.ToList();
        PrintDiagnostics(profilesToReturn);
        return profilesToReturn;
    }

    private void PrintDiagnostics(IEnumerable<ProcessedQualityProfileData> profiles)
    {
        var scoreless = profiles
            .SelectMany(x => x.ScorelessCfs)
            .Select(x => (x.Name, x.TrashId))
            .ToList();

        if (scoreless.Count == 0)
        {
            return;
        }

        foreach (var (name, trashId) in scoreless)
        {
            log.Debug("CF has no score in the guide or config YAML: {Name} ({TrashId})", name, trashId);
        }
    }

    private void AddCustomFormatScoreData(
        ProcessedQualityProfileData profile,
        QualityProfileScoreConfig scoreConfig,
        CustomFormatData cf)
    {
        var existingScoreData = profile.CfScores;

        var scoreToUse = DetermineScore(profile.Profile, scoreConfig, cf);
        if (scoreToUse is null)
        {
            profile.ScorelessCfs.Add(cf);
            return;
        }

        var existingScore = existingScoreData.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(cf.TrashId));
        if (existingScore is not null)
        {
            if (existingScore.Score != scoreToUse)
            {
                log.Warning(
                    "Custom format {Name} ({TrashId}) is duplicated in quality profile {ProfileName} with a score " +
                    "of {NewScore}, which is different from the original score of {OriginalScore}",
                    cf.Name, cf.TrashId, scoreConfig.Name, scoreToUse, existingScore.Score);
            }
            else
            {
                log.Debug("Skipping duplicate score for {Name} ({TrashId})", cf.Name, cf.TrashId);
            }

            return;
        }

        existingScoreData.Add(new ProcessedQualityProfileScore(cf.TrashId, cf.Name, cf.Id, scoreToUse.Value));
    }

    private int? DetermineScore(
        QualityProfileConfig profile,
        QualityProfileScoreConfig scoreConfig,
        CustomFormatData cf)
    {
        if (scoreConfig.Score is not null)
        {
            return scoreConfig.Score;
        }

        if (profile.ScoreSet is not null)
        {
            if (cf.TrashScores.TryGetValue(profile.ScoreSet, out var scoreFromSet))
            {
                return scoreFromSet;
            }

            log.Debug("CF {CfName} has no Score Set with name '{ScoreSetName}'", cf.Name, profile.ScoreSet);
        }

        return cf.DefaultScore;
    }
}
