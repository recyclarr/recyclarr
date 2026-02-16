namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlannedQualitySizes
{
    public required string Type { get; init; }
    public decimal? PreferredRatio { get; init; }
    public bool ResetBeforeSync { get; init; }
    public required IReadOnlyCollection<PlannedQualityItem> Qualities { get; init; }
}

internal record PlannedQualityItem(string Quality, decimal Min, decimal? Max, decimal? Preferred);
