using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Pipelines.QualityProfile.Models;

internal record AmbiguousQualityProfile(
    PlannedQualityProfile PlannedProfile,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
);
