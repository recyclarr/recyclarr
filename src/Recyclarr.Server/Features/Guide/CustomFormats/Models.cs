namespace Recyclarr.Server.Features.Guide.CustomFormats;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideCustomFormatsResponse(
    IReadOnlyList<GuideCustomFormatSummaryResponse> Items
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatSummaryResponse
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }

    /// <summary>The guide's category for the custom format; absent when it has none.</summary>
    public string? Category { get; init; }
}
