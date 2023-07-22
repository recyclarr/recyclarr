using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record ProcessedQualityProfileScore(string TrashId, string CfName, int FormatId, int Score);

public record ProcessedQualityProfileData(QualityProfileConfig Profile)
{
    public bool ShouldCreate { get; init; } = true;
    public IList<ProcessedQualityProfileScore> CfScores { get; init; } = new List<ProcessedQualityProfileScore>();
}

public class QualityProfileConfigPhase
{
    private readonly ILogger _log;
    private readonly ProcessedCustomFormatCache _cache;

    public QualityProfileConfigPhase(ILogger log, ProcessedCustomFormatCache cache)
    {
        _log = log;
        _cache = cache;
    }

    public IReadOnlyCollection<ProcessedQualityProfileData> Execute(IServiceConfiguration config)
    {
        // 1. For each group of CFs that has a quality profile specified
        // 2. For each quality profile score config in that CF group
        // 3. For each CF in the group above, match it to a Guide CF object and pair it with the quality profile config
        var profileAndCfs = config.CustomFormats
            .Where(x => x.QualityProfiles.IsNotEmpty())
            .SelectMany(x => x.QualityProfiles
                .Select(y => (Config: x, Profile: y)))
            .SelectMany(x => x.Config.TrashIds
                .Select(_cache.LookupByTrashId)
                .NotNull()
                .Select(y => (x.Profile, Cf: y)));

        var allProfiles = config.QualityProfiles
            .Select(x => new ProcessedQualityProfileData(x))
            .ToDictionary(x => x.Profile.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

        foreach (var (profile, cf) in profileAndCfs)
        {
            if (!allProfiles.TryGetValue(profile.Name, out var profileCfs))
            {
                _log.Debug("Implicitly adding quality profile config for {ProfileName}", profile.Name);

                // If the user did not specify a quality profile in their config, we still create the QP object
                // for consistency (at the very least for the name).
                allProfiles[profile.Name] = profileCfs =
                    new ProcessedQualityProfileData(new QualityProfileConfig {Name = profile.Name})
                    {
                        // The user must explicitly specify a profile in the top-level `quality_profiles` section of
                        // their config, otherwise we do not implicitly create them in the service.
                        ShouldCreate = false
                    };
            }

            AddCustomFormatScoreData(profileCfs.CfScores, profile, cf);
        }

        return allProfiles.Values.ToList();
    }

    private void AddCustomFormatScoreData(
        ICollection<ProcessedQualityProfileScore> existingScoreData,
        QualityProfileScoreConfig profile,
        CustomFormatData cf)
    {
        var scoreToUse = profile.Score ?? cf.TrashScore;
        if (scoreToUse is null)
        {
            _log.Information("No score in guide or config for CF {Name} ({TrashId})", cf.Name, cf.TrashId);
            return;
        }

        var existingScore = existingScoreData.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(cf.TrashId));
        if (existingScore is not null)
        {
            if (existingScore.Score != scoreToUse)
            {
                _log.Warning(
                    "Custom format {Name} ({TrashId}) is duplicated in quality profile {ProfileName} with a score " +
                    "of {NewScore}, which is different from the original score of {OriginalScore}",
                    cf.Name, cf.TrashId, profile.Name, scoreToUse, existingScore);
            }
            else
            {
                _log.Debug("Skipping duplicate score for {Name} ({TrashId})", cf.Name, cf.TrashId);
            }

            return;
        }

        existingScoreData.Add(new ProcessedQualityProfileScore(cf.TrashId, cf.Name, cf.Id, scoreToUse.Value));
    }
}
