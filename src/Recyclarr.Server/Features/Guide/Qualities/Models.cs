namespace Recyclarr.Server.Features.Guide.Qualities;

internal sealed record ListGuideQualitiesResponse(IReadOnlyList<GuideQualitySummaryResponse> Items);

internal sealed record GuideQualitySummaryResponse
{
    /// <summary>
    /// The name of one set of guide quality sizes (for example `movie`). Use it as
    /// `quality_definition.type` in configuration.
    /// </summary>
    public required string Type { get; init; }
}
