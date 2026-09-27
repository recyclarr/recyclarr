namespace Recyclarr.Server.Features.Guide.CustomFormats;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideCustomFormatsResponse(
    IReadOnlyList<GuideCustomFormatSummaryResponse> Items
);

// Category is the guide's grouping for the custom format; null when the guide lists none.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatSummaryResponse(
    string TrashId,
    string Name,
    string? Category
);
