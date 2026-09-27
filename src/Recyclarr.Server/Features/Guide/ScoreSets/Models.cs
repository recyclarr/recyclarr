namespace Recyclarr.Server.Features.Guide.ScoreSets;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideScoreSetsResponse(
    IReadOnlyList<GuideScoreSetSummaryResponse> Items
);

// A score set is a named column of custom format scores in the guide (for example "default").
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideScoreSetSummaryResponse(string Name);
