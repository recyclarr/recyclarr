using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal enum FormatScoreUpdateReason
{
    /// <summary>
    /// A score who's value did not change.
    /// </summary>
    NoChange,

    /// <summary>
    /// A score that is changed.
    /// </summary>
    Updated,

    /// <summary>
    /// Scores were reset to a 0 value because `reset_unmatched_scores` was set to `true`.
    /// </summary>
    Reset,

    /// <summary>
    /// New custom format scores (format items) shouldn't exist normally. They do exist during
    /// `--preview` runs since new custom formats that aren't synced yet won't be available when
    /// processing quality profiles.
    /// </summary>
    New,
}

internal record UpdatedFormatScore(
    ProfileFormatItemDto Dto,
    int NewScore,
    FormatScoreUpdateReason Reason
)
{
    public static UpdatedFormatScore New(PlannedCfScore score)
    {
        var dto = new ProfileFormatItemDto { Format = score.ServiceId, Name = score.Name };
        return new UpdatedFormatScore(dto, score.Score, FormatScoreUpdateReason.New);
    }

    public static UpdatedFormatScore Reset(
        ProfileFormatItemDto dto,
        PlannedQualityProfile profileData
    )
    {
        var reset = profileData.Config.ResetUnmatchedScores;
        var shouldReset = reset.Enabled && reset.Except.All(x => !dto.Name.EqualsIgnoreCase(x));

        var score = shouldReset ? 0 : dto.Score;
        var reason = shouldReset ? FormatScoreUpdateReason.Reset : FormatScoreUpdateReason.NoChange;
        return new UpdatedFormatScore(dto, score, reason);
    }

    public static UpdatedFormatScore Updated(ProfileFormatItemDto dto, PlannedCfScore score)
    {
        var reason =
            dto.Score == score.Score
                ? FormatScoreUpdateReason.NoChange
                : FormatScoreUpdateReason.Updated;
        return new UpdatedFormatScore(dto, score.Score, reason);
    }
}
