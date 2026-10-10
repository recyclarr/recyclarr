namespace Recyclarr.Server.Features.Guide.CustomFormatGroups;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideCustomFormatGroupsResponse(
    IReadOnlyList<GuideCustomFormatGroupSummaryResponse> Items
);

/// <summary>A guide custom format group: a set of custom formats that sync together.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatGroupSummaryResponse
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// True when the group is enabled by default for the quality profiles it applies to.
    /// </summary>
    public required bool Default { get; init; }

    /// <summary>The group's custom formats, sorted by name.</summary>
    public required IReadOnlyList<GuideCustomFormatGroupMemberResponse> CustomFormats { get; init; }

    /// <summary>Names of the guide quality profiles the group applies to.</summary>
    public required IReadOnlyList<string> QualityProfiles { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideCustomFormatGroupMemberResponse
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }

    /// <summary>True when the custom format always syncs with its group.</summary>
    public required bool Required { get; init; }

    /// <summary>True when the custom format is enabled unless the user excludes it.</summary>
    public required bool Default { get; init; }
}
