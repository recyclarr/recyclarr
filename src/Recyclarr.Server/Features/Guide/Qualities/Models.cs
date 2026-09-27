namespace Recyclarr.Server.Features.Guide.Qualities;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListGuideQualitiesResponse(IReadOnlyList<GuideQualitySummaryResponse> Items);

// Type names one set of guide quality sizes (for example "movie"); it is the value for
// quality_definition.type in configuration.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideQualitySummaryResponse(string Type);
