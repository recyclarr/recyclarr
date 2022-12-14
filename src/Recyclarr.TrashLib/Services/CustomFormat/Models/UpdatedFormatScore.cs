namespace Recyclarr.TrashLib.Services.CustomFormat.Models;

public enum FormatScoreUpdateReason
{
    Updated,
    Reset
}

public record UpdatedFormatScore(
    string CustomFormatName,
    int Score,
    FormatScoreUpdateReason Reason);
