using Recyclarr.Cli.Pipelines.QualityProfile.Api;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public enum FormatScoreUpdateReason
{
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

public record UpdatedFormatScore
{
    public required ProfileFormatItemDto Dto { get; init; }
    public required int NewScore { get; init; }
    public required FormatScoreUpdateReason Reason { get; init; }

    public void Deconstruct(out ProfileFormatItemDto dto, out int newScore, out FormatScoreUpdateReason reason)
    {
        dto = Dto;
        newScore = NewScore;
        reason = Reason;
    }
}
