using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record ProcessedQualityProfileScore(
    string TrashId,
    string CfName,
    int FormatId,
    int Score
);

internal record ProcessedQualityProfileData
{
    public required QualityProfileConfig Profile { get; init; }
    public bool ShouldCreate { get; init; } = true;
    public IList<ProcessedQualityProfileScore> CfScores { get; init; } = [];
    public IList<CustomFormatResource> ScorelessCfs { get; } = [];
}
