using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Pipelines.Plan;

/// <summary>The quality profile pipeline's plan: one entry per distinct profile name.</summary>
internal sealed record PlannedQualityProfiles(IReadOnlyCollection<PlannedQualityProfile> Items);

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

    /// <summary>
    /// True when the profile is listed under <c>quality_profiles</c>: Recyclarr owns the whole
    /// profile, creating it when missing and syncing its qualities, settings, and sizes. False
    /// when it is only named in <c>assign_scores_to</c>: it receives custom format scores only,
    /// and a missing profile is reported and skipped.
    /// </summary>
    public bool IsManaged { get; init; } = true;

    // Config overrides from user YAML
    public required QualityProfileConfig Config { get; init; }

    // CF scores: resolved from score_set or explicit config
    public IList<PlannedCfScore> CfScores { get; init; } = [];

    // Sizes assigned to this profile's qualities on services that store sizes per profile. Null
    // when no size source applies; the service's existing sizes are then left untouched.
    public PlannedQualitySizes? QualitySizes { get; init; }

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
