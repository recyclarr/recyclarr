using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record ProcessedQualityProfileScore(string TrashId, string CfName, int FormatId, int Score);

public record ProcessedQualityProfileData(QualityProfileConfig Profile)
{
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

        var allProfiles =
            new Dictionary<string, ProcessedQualityProfileData>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var (profile, cf) in profileAndCfs)
        {
            if (!allProfiles.TryGetValue(profile.Name, out var profileCfs))
            {
                profileCfs = new ProcessedQualityProfileData(
                    config.QualityProfiles.FirstOrDefault(
                        x => x.Name.EqualsIgnoreCase(profile.Name),
                        // If the user did not specify a quality profile in their config, we still create the QP object
                        // for consistency (at the very least for the name).
                        new QualityProfileConfig {Name = profile.Name}));
                allProfiles[profile.Name] = profileCfs;
            }

            AddCustomFormatScoreData(profileCfs.CfScores, profile, cf);
        }

        return allProfiles.Values
            .Where(x => x.CfScores.IsNotEmpty())
            .ToList();
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
