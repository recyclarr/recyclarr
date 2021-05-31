using System.Collections.Generic;

namespace Trash.Radarr.CustomFormat.Models
{
    public record FormatMappingEntry(ProcessedCustomFormatData CustomFormat, int Score);

    public class QualityProfileCustomFormatScoreMapping
    {
        public QualityProfileCustomFormatScoreMapping(bool resetUnmatchedScores)
        {
            ResetUnmatchedScores = resetUnmatchedScores;
        }

        public bool ResetUnmatchedScores { get; }
        public List<FormatMappingEntry> Mapping { get; init; } = new();
    }
}
