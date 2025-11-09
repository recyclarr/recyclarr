using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal record ProfileWithStats
{
    public required UpdatedQualityProfile Profile { get; set; }
    public bool ProfileChanged { get; set; }
    public bool ScoresChanged { get; set; }
    public bool QualitiesChanged { get; set; }

    public bool HasChanges => ProfileChanged || ScoresChanged || QualitiesChanged;
}

internal class QualityProfileStatCalculator(ILogger log)
{
    public ProfileWithStats Calculate(UpdatedQualityProfile profile)
    {
        log.Debug("Updates for profile {ProfileName}", profile.ProfileName);

        var stats = new ProfileWithStats { Profile = profile };
        var oldDto = profile.ProfileDto;
        var newDto = profile.BuildUpdatedDto();

        ProfileUpdates(stats, oldDto, newDto);
        QualityUpdates(stats, oldDto, newDto);
        ScoreUpdates(stats, profile.ProfileDto, profile.UpdatedScores);

        return stats;
    }

    private void ProfileUpdates(
        ProfileWithStats stats,
        QualityProfileDto oldDto,
        QualityProfileDto newDto
    )
    {
        Log("Upgrade Allowed", oldDto.UpgradeAllowed, newDto.UpgradeAllowed);
        Log(
            "Cutoff",
            oldDto.Items.FindCutoff(oldDto.Cutoff),
            newDto.Items.FindCutoff(newDto.Cutoff)
        );
        Log("Cutoff Score", oldDto.CutoffFormatScore, newDto.CutoffFormatScore);
        Log("Minimum Score", oldDto.MinFormatScore, newDto.MinFormatScore);
        Log("Minimum Upgrade Score", oldDto.MinUpgradeFormatScore, newDto.MinUpgradeFormatScore);

        return;

        void Log<T>(string msg, T oldValue, T newValue)
        {
            log.Debug("{Msg}: {Old} -> {New}", msg, oldValue, newValue);
            stats.ProfileChanged |= !EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }
    }

    private static void QualityUpdates(
        ProfileWithStats stats,
        QualityProfileDto oldDto,
        QualityProfileDto newDto
    )
    {
        using var oldJson = JsonSerializer.SerializeToDocument(oldDto.Items);
        using var newJson = JsonSerializer.SerializeToDocument(newDto.Items);
        stats.QualitiesChanged =
            stats.Profile.MissingQualities.Count > 0 || !oldJson.DeepEquals(newJson);
    }

    private void ScoreUpdates(
        ProfileWithStats stats,
        QualityProfileDto profileDto,
        IReadOnlyCollection<UpdatedFormatScore> updatedScores
    )
    {
        var scores = updatedScores.Where(y => y.Dto.Score != y.NewScore).ToList();

        if (scores.Count == 0)
        {
            return;
        }

        log.Debug("> Scores updated for quality profile: {ProfileName}", profileDto.Name);

        foreach (var (dto, newScore, reason) in scores)
        {
            log.Debug(
                "  - {Name} ({Id}): {OldScore} -> {NewScore} ({Reason})",
                dto.Name,
                dto.Format,
                dto.Score,
                newScore,
                reason
            );
        }

        stats.ScoresChanged = true;
    }
}
