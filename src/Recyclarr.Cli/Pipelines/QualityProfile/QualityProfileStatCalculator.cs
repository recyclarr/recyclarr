using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using Recyclarr.Servarr.QualityProfile;

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

        var oldProfile = profile.Profile;
        var newProfile = profile.BuildMergedProfile();

        return new ProfileWithStats
        {
            Profile = profile,
            ProfileChanged = CheckProfileChanges(oldProfile, newProfile),
            QualitiesChanged = CheckQualityChanges(profile, oldProfile, newProfile),
            ScoresChanged = CheckScoreChanges(profile.Profile, profile.UpdatedScores),
        };
    }

    private bool CheckProfileChanges(QualityProfileData oldProfile, QualityProfileData newProfile)
    {
        var changed = false;

        Check("Name", oldProfile.Name, newProfile.Name);
        Check("Upgrade Allowed", oldProfile.UpgradeAllowed, newProfile.UpgradeAllowed);
        Check(
            "Cutoff",
            oldProfile.Items.FindCutoff(oldProfile.Cutoff),
            newProfile.Items.FindCutoff(newProfile.Cutoff)
        );
        Check("Cutoff Score", oldProfile.CutoffFormatScore, newProfile.CutoffFormatScore);
        Check("Minimum Score", oldProfile.MinFormatScore, newProfile.MinFormatScore);
        Check(
            "Minimum Upgrade Score",
            oldProfile.MinUpgradeFormatScore,
            newProfile.MinUpgradeFormatScore
        );

        return changed;

        void Check<T>(string msg, T oldValue, T newValue)
        {
            log.Debug("{Msg}: {Old} -> {New}", msg, oldValue, newValue);
            changed |= !EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }
    }

    private static bool CheckQualityChanges(
        UpdatedQualityProfile profile,
        QualityProfileData oldProfile,
        QualityProfileData newProfile
    )
    {
        using var oldJson = JsonSerializer.SerializeToDocument(oldProfile.Items);
        using var newJson = JsonSerializer.SerializeToDocument(newProfile.Items);
        return profile.MissingQualities.Count > 0 || !oldJson.DeepEquals(newJson);
    }

    private bool CheckScoreChanges(
        QualityProfileData profile,
        IReadOnlyCollection<UpdatedFormatScore> updatedScores
    )
    {
        var scores = updatedScores.Where(y => y.FormatItem.Score != y.NewScore).ToList();

        if (scores.Count == 0)
        {
            return false;
        }

        log.Debug("> Scores updated for quality profile: {ProfileName}", profile.Name);

        foreach (var (formatItem, newScore, reason) in scores)
        {
            log.Debug(
                "  - {Name} ({Id}): {OldScore} -> {NewScore} ({Reason})",
                formatItem.Name,
                formatItem.FormatId,
                formatItem.Score,
                newScore,
                reason
            );
        }

        return true;
    }
}
