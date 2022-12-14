namespace Recyclarr.TrashLib.Services.CustomFormat.Models;

public record FormatMappingEntry(ProcessedCustomFormatData CustomFormat, int Score);

public class QualityProfileCustomFormatScoreMapping
{
    public QualityProfileCustomFormatScoreMapping(bool resetUnmatchedScores)
    {
        ResetUnmatchedScores = resetUnmatchedScores;
    }

    public bool ResetUnmatchedScores { get; }
    public ICollection<FormatMappingEntry> Mapping { get; init; } = new List<FormatMappingEntry>();
}
