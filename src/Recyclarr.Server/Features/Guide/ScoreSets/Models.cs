namespace Recyclarr.Server.Features.Guide.ScoreSets;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideScoreSetsResponse(
    IReadOnlyList<GuideScoreSetSummaryResponse> Items
);

/// <summary>
/// A named set of custom format scores in the guide (for example `default`). A quality profile
/// selects one set for its scores.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideScoreSetSummaryResponse(string Name);
