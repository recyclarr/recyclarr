using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupSelectReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupExcludeReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcomeResponse(
    string GroupTrashId,
    string ProfileTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatQualityProfileReferenceAmbiguousPlanningOutcomeResponse(
    string ProfileTrashId,
    IReadOnlyList<string> ProfileNames
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcomeResponse(
    string GroupTrashId,
    string ProfileTrashId,
    IReadOnlyList<string> ProfileNames
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupRequiredItemSelectedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupDefaultItemSelectedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupRequiredItemExcludedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatGroupNonDefaultItemExcludedPlanningOutcomeResponse(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcomeResponse;
