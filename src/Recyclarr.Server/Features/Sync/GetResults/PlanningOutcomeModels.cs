using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>
/// A configuration problem found before the instance's pipelines ran. The `type` property names
/// the problem. Reference mismatches and ambiguous references block every pipeline of the
/// instance; the other types are warnings.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(
    typeof(CustomFormatGroupReferenceMismatchPlanningOutcomeResponse),
    "customFormatGroupReferenceMismatch"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupSelectReferenceMismatchPlanningOutcomeResponse),
    "customFormatGroupSelectReferenceMismatch"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupExcludeReferenceMismatchPlanningOutcomeResponse),
    "customFormatGroupExcludeReferenceMismatch"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcomeResponse),
    "customFormatGroupQualityProfileReferenceMismatch"
)]
[JsonDerivedType(
    typeof(CustomFormatQualityProfileReferenceAmbiguousPlanningOutcomeResponse),
    "customFormatQualityProfileReferenceAmbiguous"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcomeResponse),
    "customFormatGroupQualityProfileReferenceAmbiguous"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupRequiredItemSelectedPlanningOutcomeResponse),
    "customFormatGroupRequiredItemSelected"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupDefaultItemSelectedPlanningOutcomeResponse),
    "customFormatGroupDefaultItemSelected"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupRequiredItemExcludedPlanningOutcomeResponse),
    "customFormatGroupRequiredItemExcluded"
)]
[JsonDerivedType(
    typeof(CustomFormatGroupNonDefaultItemExcludedPlanningOutcomeResponse),
    "customFormatGroupNonDefaultItemExcluded"
)]
internal abstract record PlanningOutcomeResponse;

/// <summary>Blocking. The configured custom format group does not exist in the guide.</summary>
internal sealed record CustomFormatGroupReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId
) : PlanningOutcomeResponse;

/// <summary>Blocking. A custom format selected from a group is not in that group.</summary>
internal sealed record CustomFormatGroupSelectReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

/// <summary>Blocking. A custom format excluded from a group is not in that group.</summary>
internal sealed record CustomFormatGroupExcludeReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

/// <summary>
/// Blocking. A quality profile that a group assigns scores to does not exist in the guide.
/// </summary>
internal sealed record CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string ProfileTrashId
) : PlanningOutcomeResponse;

/// <summary>
/// Blocking. A quality profile trash ID that custom formats assign scores to matches more than one
/// configured profile.
/// </summary>
internal sealed record CustomFormatQualityProfileReferenceAmbiguousPlanningOutcomeResponse(
    string ProfileTrashId,
    IReadOnlyList<string> ProfileNames
) : PlanningOutcomeResponse;

/// <summary>
/// Blocking. A quality profile trash ID that a group assigns scores to matches more than one
/// configured profile.
/// </summary>
internal sealed record CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcomeResponse(
    string GroupTrashId,
    string ProfileTrashId,
    IReadOnlyList<string> ProfileNames
) : PlanningOutcomeResponse;

/// <summary>
/// Warning. A required group member was selected; the group always includes it.
/// </summary>
internal sealed record CustomFormatGroupRequiredItemSelectedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

/// <summary>
/// Warning. A default group member was selected; the group includes it by default.
/// </summary>
internal sealed record CustomFormatGroupDefaultItemSelectedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

/// <summary>
/// Warning. A required group member was excluded; required members cannot be excluded.
/// </summary>
internal sealed record CustomFormatGroupRequiredItemExcludedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

/// <summary>
/// Warning. A non-default group member was excluded without selecting all optional members, so
/// the exclusion has no effect.
/// </summary>
internal sealed record CustomFormatGroupNonDefaultItemExcludedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;
