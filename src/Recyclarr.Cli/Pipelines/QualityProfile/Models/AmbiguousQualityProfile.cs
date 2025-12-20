using Recyclarr.Cli.Pipelines.Plan;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record AmbiguousQualityProfile(
    PlannedQualityProfile PlannedProfile,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
);
