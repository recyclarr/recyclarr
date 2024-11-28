using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

public record ProcessedQualityProfileScore(string TrashId, string CfName, int FormatId, int Score);

public record ProcessedQualityProfileData
{
    public required QualityProfileConfig Profile { get; init; }
    public bool ShouldCreate { get; init; } = true;
    public IList<ProcessedQualityProfileScore> CfScores { get; init; } = [];
    public IList<CustomFormatData> ScorelessCfs { get; } = [];
}
