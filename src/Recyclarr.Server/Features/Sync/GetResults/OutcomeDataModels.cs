namespace Recyclarr.Server.Features.Sync.GetResults;

// The data shapes of outcome codes. Codes reference these through OutcomeCodes; the OpenAPI
// document publishes them as schemas because no endpoint references them directly.

/// <summary>A custom format named in a group's configuration.</summary>
internal sealed record CustomFormatGroupMemberOutcomeData(string CustomFormatTrashId);

/// <summary>A quality profile trash ID that a group assigns scores to.</summary>
internal sealed record QualityProfileReferenceOutcomeData(string ProfileTrashId);

/// <summary>The configured profiles that one quality profile trash ID matches.</summary>
internal sealed record ProfileNamesOutcomeData(IReadOnlyList<string> ProfileNames);

/// <summary>
/// A quality profile trash ID that a group assigns scores to, and the configured profiles it
/// matches.
/// </summary>
internal sealed record QualityProfileReferenceAmbiguousOutcomeData(
    string ProfileTrashId,
    IReadOnlyList<string> ProfileNames
);

/// <summary>The trash IDs of the custom formats that share one name.</summary>
internal sealed record CustomFormatDuplicateNameOutcomeData(IReadOnlyList<string> TrashIds);

/// <summary>The ID of the service resource that Recyclarr now manages.</summary>
internal sealed record ServiceIdOutcomeData(int ServiceId);

/// <summary>The service resources whose names match.</summary>
internal sealed record ServiceMatchesOutcomeData(
    IReadOnlyList<NamedServiceResourceResponse> ServiceMatches
);

/// <summary>The managed custom format that uses `serviceId` now.</summary>
internal sealed record CustomFormatStateConflictOutcomeData(
    TrashIdNameResponse ManagedIdentity,
    int ServiceId
);

/// <summary>
/// The minimum custom format score, the sum of positive scores, and the highest score.
/// </summary>
internal sealed record MinimumScoreOutcomeData(
    int MinimumScore,
    int TotalPositiveScore,
    int MaximumScore
);

internal sealed record QualityNameOutcomeData(string Quality);

internal sealed record QualityNamesOutcomeData(IReadOnlyList<string> Qualities);

/// <summary>
/// `reset_unmatched_scores.except` names that are not in the profile, and except patterns that
/// match no custom format in the profile.
/// </summary>
internal sealed record ResetScoreReferencesOutcomeData(
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Patterns
);

/// <summary>The service profile that already has the new name.</summary>
internal sealed record RenameBlockedOutcomeData(NamedServiceResourceResponse Conflict);

/// <summary>A configured `quality_definition.type`.</summary>
internal sealed record QualityDefinitionTypeOutcomeData(string Type);

internal sealed record QualitySizeReferenceOutcomeData(string Quality, string Type);

/// <summary>A configured preferred ratio outside 0 to 1, and the value used instead.</summary>
internal sealed record PreferredRatioClampedOutcomeData(decimal Configured, decimal Used);

internal sealed record MinimumComparisonOutcomeData(
    string Quality,
    QualitySizeValueResponse Minimum,
    QualitySizeValueResponse Preferred
);

internal sealed record MaximumComparisonOutcomeData(
    string Quality,
    QualitySizeValueResponse Preferred,
    QualitySizeValueResponse Maximum
);

/// <summary>A quality size set that a guide quality profile links by trash ID.</summary>
internal sealed record QualitySizeGuideReferenceOutcomeData(string Label, string TrashId);

/// <summary>The quality size sets that one guide quality profile links.</summary>
internal sealed record QualitySizeGuideLabelsOutcomeData(IReadOnlyList<string> Labels);

/// <summary>
/// A media naming field, for example `seriesFolderFormat`, and the configured key that matches
/// no guide format for it.
/// </summary>
internal sealed record MediaNamingReferenceOutcomeData(string Field, string ConfiguredKey);
