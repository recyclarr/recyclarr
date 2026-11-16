namespace Recyclarr.Server.Features.Guide.CustomFormats;

internal sealed record ListGuideCustomFormatsResponse(
    IReadOnlyList<GuideCustomFormatSummaryResponse> Items
);

internal sealed record GuideCustomFormatSummaryResponse
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }

    /// <summary>The guide's category for the custom format; absent when it has none.</summary>
    public string? Category { get; init; }
}
