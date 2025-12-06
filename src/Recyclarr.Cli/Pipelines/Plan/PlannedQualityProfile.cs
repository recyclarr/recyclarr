using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlannedQualityProfile
{
    public required string Name { get; init; }

    // True when profile is explicitly declared under `quality_profiles:` in YAML config.
    // False when profile is only referenced via `assign_scores_to:` (implicit).
    // Transaction phase uses this to decide: create missing profile (true) or warn and skip (false).
    public bool ShouldCreate { get; init; } = true;

    // Config overrides from user YAML
    public required QualityProfileConfig Config { get; init; }

    // CF scores: resolved from score_set or explicit config
    public IList<PlannedCfScore> CfScores { get; init; } = [];
}

// Holds a reference to PlannedCustomFormat (not just TrashId) to enable ID hydration.
// When CF persistence creates a new CF and sets Resource.Id, QP scores see the update
// automatically via the shared object reference - no explicit hydration step needed.
internal record PlannedCfScore(PlannedCustomFormat CustomFormat, int Score)
{
    public string TrashId => CustomFormat.Resource.TrashId;
    public string Name => CustomFormat.Resource.Name;
    public int ServiceId => CustomFormat.Resource.Id;
}
