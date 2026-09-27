namespace Recyclarr.Server.Features.Guide.QualityProfiles;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideQualityProfilesResponse(
    IReadOnlyList<GuideQualityProfileSummaryResponse> Items
);

// ScoreSet is null when the profile uses the default score set. AllowedQualities lists only the
// qualities the profile allows, in guide order. CustomFormats are the names the profile scores.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideQualityProfileSummaryResponse(
    string TrashId,
    string Name,
    string? TrashUrl,
    string? ScoreSet,
    IReadOnlyList<GuideProfileQualityResponse> AllowedQualities,
    IReadOnlyList<string> CustomFormats
);

// Items lists the member qualities when the entry is a quality group.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideProfileQualityResponse(string Name, IReadOnlyList<string> Items);
