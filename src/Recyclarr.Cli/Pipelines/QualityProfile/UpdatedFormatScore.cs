using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public enum FormatScoreUpdateReason
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
    New
}

public record UpdatedFormatScore(ProfileFormatItemDto Dto, int NewScore, FormatScoreUpdateReason Reason)
{
    public static UpdatedFormatScore New(ProcessedQualityProfileScore score)
    {
        var dto = new ProfileFormatItemDto {Format = score.FormatId, Name = score.CfName};
        return new UpdatedFormatScore(dto, score.Score, FormatScoreUpdateReason.New);
    }

    public static UpdatedFormatScore Reset(ProfileFormatItemDto dto, ProcessedQualityProfileData profileData)
    {
        var score = profileData.Profile.ResetUnmatchedScores ? 0 : dto.Score;
        return new UpdatedFormatScore(dto, score, FormatScoreUpdateReason.Reset);
    }

    public static UpdatedFormatScore Updated(ProfileFormatItemDto dto, ProcessedQualityProfileScore score)
    {
        var reason = dto.Score == score.Score ? FormatScoreUpdateReason.NoChange : FormatScoreUpdateReason.Updated;
        return new UpdatedFormatScore(dto, score.Score, reason);
    }
}
