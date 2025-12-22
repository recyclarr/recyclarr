using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal record ProfileWithStats
{
    public required UpdatedQualityProfile Profile { get; init; }
    public bool ProfileChanged { get; init; }
    public bool ScoresChanged { get; init; }
    public bool QualitiesChanged { get; init; }
}

internal class QualityProfileStatCalculator(ILogger log)
{
    public ProfileWithStats Calculate(UpdatedQualityProfile profile)
    {
        log.Debug("Updates for profile {ProfileName}", profile.ProfileName);

        var oldDto = profile.ProfileDto;
        var newDto = profile.BuildUpdatedDto();

        return new ProfileWithStats
        {
            Profile = profile,
            ProfileChanged = CheckProfileChanges(oldDto, newDto),
            QualitiesChanged = CheckQualityChanges(profile, oldDto, newDto),
            ScoresChanged = CheckScoreChanges(profile.ProfileDto, profile.UpdatedScores),
        };
    }

    private bool CheckProfileChanges(QualityProfileDto oldDto, QualityProfileDto newDto)
    {
        var changed = false;

        Check("Upgrade Allowed", oldDto.UpgradeAllowed, newDto.UpgradeAllowed);
        Check(
            "Cutoff",
            oldDto.Items.FindCutoff(oldDto.Cutoff),
            newDto.Items.FindCutoff(newDto.Cutoff)
        );
        Check("Cutoff Score", oldDto.CutoffFormatScore, newDto.CutoffFormatScore);
        Check("Minimum Score", oldDto.MinFormatScore, newDto.MinFormatScore);
        Check("Minimum Upgrade Score", oldDto.MinUpgradeFormatScore, newDto.MinUpgradeFormatScore);

        return changed;

        void Check<T>(string msg, T oldValue, T newValue)
        {
            log.Debug("{Msg}: {Old} -> {New}", msg, oldValue, newValue);
            changed |= !EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }
    }

    private static bool CheckQualityChanges(
        UpdatedQualityProfile profile,
        QualityProfileDto oldDto,
        QualityProfileDto newDto
    )
    {
        using var oldJson = JsonSerializer.SerializeToDocument(oldDto.Items);
        using var newJson = JsonSerializer.SerializeToDocument(newDto.Items);
        return profile.MissingQualities.Count > 0 || !oldJson.DeepEquals(newJson);
    }

    private bool CheckScoreChanges(
        QualityProfileDto profileDto,
        IReadOnlyCollection<UpdatedFormatScore> updatedScores
    )
    {
        var scores = updatedScores.Where(y => y.Dto.Score != y.NewScore).ToList();

        if (scores.Count == 0)
        {
            return false;
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

        return true;
    }
}
