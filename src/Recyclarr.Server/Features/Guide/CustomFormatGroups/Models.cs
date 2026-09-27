namespace Recyclarr.Server.Features.Guide.CustomFormatGroups;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideCustomFormatGroupsResponse(
    IReadOnlyList<GuideCustomFormatGroupSummaryResponse> Items
);

// Default: the group is enabled by default for the quality profiles it includes.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatGroupSummaryResponse(
    string TrashId,
    string Name,
    bool Default,
    IReadOnlyList<GuideCustomFormatGroupMemberResponse> CustomFormats,
    IReadOnlyList<string> QualityProfiles
);

// Required members are always synced with the group; Default members are enabled unless the
// user opts out.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatGroupMemberResponse(
    string TrashId,
    string Name,
    bool Required,
    bool Default
);
