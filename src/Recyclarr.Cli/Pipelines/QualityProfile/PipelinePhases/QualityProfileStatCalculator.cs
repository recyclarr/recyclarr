using Newtonsoft.Json.Linq;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record ProfileWithStats
{
    public required UpdatedQualityProfile Profile { get; set; }
    public bool ProfileChanged { get; set; }
    public bool ScoresChanged { get; set; }
    public bool QualitiesChanged { get; set; }

    public bool HasChanges => ProfileChanged || ScoresChanged || QualitiesChanged;
}

public class QualityProfileStatCalculator
{
    private readonly ILogger _log;

    public QualityProfileStatCalculator(ILogger log)
    {
        _log = log;
    }

    public ProfileWithStats Calculate(UpdatedQualityProfile profile)
    {
        _log.Debug("Updates for profile {ProfileName}", profile.ProfileName);

        var stats = new ProfileWithStats {Profile = profile};

        ProfileUpdates(stats, profile);
        QualityUpdates(stats, profile);
        ScoreUpdates(stats, profile.ProfileDto, profile.UpdatedScores);

        return stats;
    }

    private void ProfileUpdates(ProfileWithStats stats, UpdatedQualityProfile profile)
    {
        var dto = profile.ProfileDto;
        var config = profile.ProfileConfig.Profile;

        void Log<T>(string msg, T oldValue, T newValue)
        {
            _log.Debug("{Msg}: {Old} -> {New}", msg, oldValue, newValue);
            stats.ProfileChanged |= !EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }

        var upgradeAllowed = config.UpgradeAllowed is not null;
        Log("Upgrade Allowed", dto.UpgradeAllowed, upgradeAllowed);

        if (upgradeAllowed)
        {
            Log("Cutoff", dto.Items.FindCutoff(dto.Cutoff), config.UpgradeUntilQuality);
            Log("Cutoff Score", dto.CutoffFormatScore, config.UpgradeUntilScore);
        }

        Log("Minimum Score", dto.MinFormatScore, config.MinFormatScore);
    }

    private static void QualityUpdates(ProfileWithStats stats, UpdatedQualityProfile profile)
    {
        var dtoQualities = JToken.FromObject(profile.ProfileDto.Items);
        var updatedQualities = JToken.FromObject(profile.UpdatedQualities.Items);
        stats.QualitiesChanged = !JToken.DeepEquals(dtoQualities, updatedQualities);
    }

    private void ScoreUpdates(
        ProfileWithStats stats,
        QualityProfileDto profileDto,
        IReadOnlyCollection<UpdatedFormatScore> updatedScores)
    {
        var scores = updatedScores
            .Where(y => y.Dto.Score != y.NewScore)
            .ToList();

        if (scores.Count == 0)
        {
            return;
        }

        _log.Debug("> Scores updated for quality profile: {ProfileName}", profileDto.Name);

        foreach (var (dto, newScore, reason) in scores)
        {
            _log.Debug("  - {Format} ({Id}): {OldScore} -> {NewScore} ({Reason})",
                dto.Name, dto.Format, dto.Score, newScore, reason);
        }

        stats.ScoresChanged = true;
    }
}
