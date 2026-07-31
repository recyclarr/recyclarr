using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Pipelines.Plan;

// Discriminated union over the two kinds of quality profiles we plan:
//   GuideBacked: sourced from a TRaSH Guides resource (trash_id). Resource is always present.
//   UserDefined: user-authored, no guide backing.
//
// Callers that need guide-specific behavior pattern match on the variant. Callers that treat
// the guide resource as an optional fallback (e.g., effective-value resolution) use the
// GuideResource property.
internal abstract record PlannedQualityProfile
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

    public QualityProfileResource? GuideResource =>
        this is GuideBacked guideBacked ? guideBacked.Resource : null;

    internal sealed record GuideBacked : PlannedQualityProfile
    {
        public required QualityProfileResource Resource { get; init; }
    }

    internal sealed record UserDefined : PlannedQualityProfile;
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
