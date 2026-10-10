namespace Recyclarr.Server.Features.Guide.QualityProfiles;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideQualityProfilesResponse(
    IReadOnlyList<GuideQualityProfileSummaryResponse> Items
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideQualityProfileSummaryResponse
{
    public required string TrashId { get; init; }
    public required string Name { get; init; }

    /// <summary>Link to the guide page for the profile; absent when the guide has none.</summary>
    public string? TrashUrl { get; init; }

    /// <summary>The score set the profile uses; absent when it uses the default set.</summary>
    public string? ScoreSet { get; init; }

    /// <summary>The qualities and quality groups the profile allows, in guide order.</summary>
    public required IReadOnlyList<GuideProfileQualityResponse> AllowedQualities { get; init; }

    /// <summary>Names of the custom formats the profile scores, sorted by name.</summary>
    public required IReadOnlyList<string> CustomFormats { get; init; }
}

/// <summary>A quality or quality group in a guide quality profile.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideProfileQualityResponse
{
    public required string Name { get; init; }

    /// <summary>The member qualities of a quality group; empty for a single quality.</summary>
    public required IReadOnlyList<string> Items { get; init; }
}
