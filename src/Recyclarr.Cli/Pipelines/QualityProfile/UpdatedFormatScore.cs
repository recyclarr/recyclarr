namespace Recyclarr.Cli.Pipelines.QualityProfile;

public enum FormatScoreUpdateReason
{
    Updated,
    Reset
}

public record UpdatedFormatScore(
    string CustomFormatName,
    int OldScore,
    int NewScore,
    FormatScoreUpdateReason Reason);
