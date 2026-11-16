using System.Globalization;
using System.Text.Json;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Sync.Results;
using C = Recyclarr.Server.Sync.Results.OutcomeCodes;

namespace Recyclarr.Server.Sync.Results;

/// <summary>
/// Maps every Core outcome to the outcome envelope: its code, the message people read, the subject,
/// and the code's data. The API and notifications both render outcomes from here, so wording and
/// severity stay the same in every channel (ADR-016, ADR-033).
/// </summary>
internal static class OutcomeResponseMapper
{
    private const string CustomFormatKind = "customFormat";
    private const string CustomFormatGroupKind = "customFormatGroup";
    private const string QualityProfileKind = "qualityProfile";

    public static OutcomeResponse Map(PlanningOutcome outcome) =>
        outcome switch
        {
            CustomFormatReferenceMismatchPlanningOutcome x =>
                C.CustomFormatReferenceMismatch.Create(
                    $"Custom Format '{x.CustomFormatTrashId}' does not exist in the guide",
                    CustomFormat(x.CustomFormatTrashId)
                ),
            CustomFormatGroupReferenceMismatchPlanningOutcome x =>
                C.CustomFormatGroupReferenceMismatch.Create(
                    $"Custom Format group '{x.GroupTrashId}' does not exist in the guide",
                    Group(x.GroupTrashId)
                ),
            CustomFormatGroupSkipReferenceMismatchPlanningOutcome x =>
                C.CustomFormatGroupSkipReferenceMismatch.Create(
                    $"Skip ID '{x.GroupTrashId}' in `custom_format_groups.skip` does not match "
                        + "any Custom Format group",
                    Group(x.GroupTrashId)
                ),
            CustomFormatGroupSelectReferenceMismatchPlanningOutcome x =>
                C.CustomFormatGroupSelectReferenceMismatch.Create(
                    $"Custom Format group '{x.GroupTrashId}' does not contain selected format "
                        + $"'{x.CustomFormatTrashId}'",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            CustomFormatGroupExcludeReferenceMismatchPlanningOutcome x =>
                C.CustomFormatGroupExcludeReferenceMismatch.Create(
                    $"Custom Format group '{x.GroupTrashId}' does not contain excluded format "
                        + $"'{x.CustomFormatTrashId}'",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcome x =>
                C.CustomFormatGroupQualityProfileReferenceMismatch.Create(
                    $"Custom Format group '{x.GroupTrashId}' assigns scores to quality profile "
                        + $"'{x.ProfileTrashId}', which does not exist in the guide",
                    Group(x.GroupTrashId),
                    new QualityProfileReferenceOutcomeData(x.ProfileTrashId)
                ),
            CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome x =>
                C.CustomFormatQualityProfileReferenceAmbiguous.Create(
                    $"Quality profile trash_id '{x.ProfileTrashId}' matches multiple configured "
                        + $"profiles: {Names(x.ProfileNames)}. Use `name` to choose one",
                    new OutcomeSubjectResponse
                    {
                        Kind = QualityProfileKind,
                        TrashId = x.ProfileTrashId,
                    },
                    new ProfileNamesOutcomeData(x.ProfileNames)
                ),
            CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome x =>
                C.CustomFormatGroupQualityProfileReferenceAmbiguous.Create(
                    $"Custom Format group '{x.GroupTrashId}' assigns scores to quality profile "
                        + $"trash_id '{x.ProfileTrashId}', which matches multiple configured "
                        + $"profiles: {Names(x.ProfileNames)}. Use `name` to choose one",
                    Group(x.GroupTrashId),
                    new QualityProfileReferenceAmbiguousOutcomeData(
                        x.ProfileTrashId,
                        x.ProfileNames
                    )
                ),
            CustomFormatGroupRequiredItemSelectedPlanningOutcome x =>
                C.CustomFormatGroupRequiredItemSelected.Create(
                    $"Selecting required Custom Format '{x.CustomFormatTrashId}' in group "
                        + $"'{x.GroupTrashId}' is redundant (required formats are always included)",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            CustomFormatGroupDefaultItemSelectedPlanningOutcome x =>
                C.CustomFormatGroupDefaultItemSelected.Create(
                    $"Selecting default Custom Format '{x.CustomFormatTrashId}' in group "
                        + $"'{x.GroupTrashId}' is redundant (default formats are already included)",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            CustomFormatGroupRequiredItemExcludedPlanningOutcome x =>
                C.CustomFormatGroupRequiredItemExcluded.Create(
                    $"Excluding required Custom Format '{x.CustomFormatTrashId}' from group "
                        + $"'{x.GroupTrashId}' has no effect (required formats are always "
                        + "included)",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            CustomFormatGroupNonDefaultItemExcludedPlanningOutcome x =>
                C.CustomFormatGroupNonDefaultItemExcluded.Create(
                    $"Excluding non-default Custom Format '{x.CustomFormatTrashId}' from group "
                        + $"'{x.GroupTrashId}' has no effect unless `select_all` is set",
                    Group(x.GroupTrashId),
                    new CustomFormatGroupMemberOutcomeData(x.CustomFormatTrashId)
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    public static OutcomeResponse Map(CustomFormatOutcome outcome) =>
        outcome switch
        {
            IncompatibleCustomFormatGroupOutcome x => C.CustomFormatGroupIncompatible.Create(
                $"Custom Format group '{x.Name}' ({x.TrashId}) was skipped because none of your "
                    + "quality profiles are in its compatibility list. Add `assign_scores_to` to "
                    + "target a profile",
                Group(x.TrashId, x.Name)
            ),
            EmptyCustomFormatGroupOutcome x => C.CustomFormatGroupEmpty.Create(
                $"Custom Format group '{x.Name}' ({x.TrashId}) has no custom formats after "
                    + "applying opt-in semantics. Use `select` to pick formats to include",
                Group(x.TrashId, x.Name)
            ),
            CustomFormatDuplicateNameOutcome x => C.CustomFormatDuplicateName.Create(
                $"Custom Format name '{x.Name}' is used by multiple trash IDs "
                    + $"({string.Join(", ", x.TrashIds)}), so none of them sync",
                new OutcomeSubjectResponse { Kind = CustomFormatKind, Name = x.Name },
                new CustomFormatDuplicateNameOutcomeData(x.TrashIds)
            ),
            CustomFormatAdoptedOutcome x => C.CustomFormatAdopted.Create(
                $"Custom Format {Identity(x.Identity)} already existed in the service (ID "
                    + $"{x.ServiceId}); Recyclarr now manages it",
                CustomFormat(x.Identity),
                new ServiceIdOutcomeData(x.ServiceId)
            ),
            CustomFormatAmbiguousMatchOutcome x => C.CustomFormatAmbiguousMatch.Create(
                $"Custom Format {Identity(x.Identity)} cannot sync because multiple service "
                    + $"formats match its name: {Matches(x.ServiceMatches.Select(y => (y.Name, y.ServiceId)))}. "
                    + "Delete or rename the duplicates in the service",
                CustomFormat(x.Identity),
                new ServiceMatchesOutcomeData(
                    x.ServiceMatches.Select(y => new NamedServiceResourceResponse(
                            y.Name,
                            y.ServiceId
                        ))
                        .ToList()
                )
            ),
            CustomFormatStateConflictOutcome x => C.CustomFormatStateConflict.Create(
                $"Custom Format {Identity(x.Identity)} conflicts with managed format "
                    + $"{Identity(x.ManagedIdentity)} at service ID {x.ServiceId}",
                CustomFormat(x.Identity),
                new CustomFormatStateConflictOutcomeData(
                    new TrashIdNameResponse(x.ManagedIdentity.TrashId, x.ManagedIdentity.Name),
                    x.ServiceId
                )
            ),
            CustomFormatCreateRejectedOutcome x => C.CustomFormatCreateRejected.Create(
                $"The service rejected creation of Custom Format {Identity(x.Identity)}",
                CustomFormat(x.Identity)
            ),
            CustomFormatUpdateRejectedOutcome x => C.CustomFormatUpdateRejected.Create(
                $"The service rejected update of Custom Format {Identity(x.Identity)}",
                CustomFormat(x.Identity)
            ),
            CustomFormatDeleteRejectedOutcome x => C.CustomFormatDeleteRejected.Create(
                $"The service rejected deletion of Custom Format {Identity(x.Identity)}",
                CustomFormat(x.Identity)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    public static OutcomeResponse Map(QualityProfileOutcome outcome) =>
        outcome switch
        {
            QualityProfileReferenceMismatchOutcome x => C.QualityProfileReferenceMismatch.Create(
                $"Quality profile trash_id '{x.TrashId}' does not exist in the guide",
                new OutcomeSubjectResponse { Kind = QualityProfileKind, TrashId = x.TrashId }
            ),
            QualityProfileDuplicateNameOutcome x => C.QualityProfileDuplicateName.Create(
                $"Quality profile name '{x.Name}' is configured more than once; each quality "
                    + "profile must have a unique name",
                new OutcomeSubjectResponse { Kind = QualityProfileKind, Name = x.Name }
            ),
            QualityProfileNotFoundOutcome x => C.QualityProfileNotFound.Create(
                $"Quality profile {Profile(x.Identity)} does not exist in the service. Add it "
                    + "under `quality_profiles` so Recyclarr can create it",
                ProfileSubject(x.Identity)
            ),
            QualityProfileAdoptedOutcome x => C.QualityProfileAdopted.Create(
                $"Quality profile {Profile(x.Identity)} already existed in the service (ID "
                    + $"{x.ServiceId}); Recyclarr now manages it",
                ProfileSubject(x.Identity),
                new ServiceIdOutcomeData(x.ServiceId)
            ),
            QualityProfileMinimumScoreUnsatisfiedOutcome x =>
                C.QualityProfileMinimumScoreUnsatisfied.Create(
                    $"Quality profile {Profile(x.Identity)}: minimum Custom Format score "
                        + $"{x.MinimumScore} can never be satisfied; the positive scores total "
                        + $"{x.TotalPositiveScore} and the highest score is {x.MaximumScore}",
                    ProfileSubject(x.Identity),
                    new MinimumScoreOutcomeData(
                        x.MinimumScore,
                        x.TotalPositiveScore,
                        x.MaximumScore
                    )
                ),
            QualityProfileInvalidCutoffOutcome x => C.QualityProfileInvalidCutoff.Create(
                $"Quality profile {Profile(x.Identity)}: `until_quality` references unknown "
                    + $"quality '{x.QualityName}'",
                ProfileSubject(x.Identity),
                new QualityNameOutcomeData(x.QualityName)
            ),
            QualityProfileUnavailableCutoffOutcome x => C.QualityProfileUnavailableCutoff.Create(
                $"Quality profile {Profile(x.Identity)}: `until_quality` '{x.QualityName}' must "
                    + "refer to an existing and enabled quality or group",
                ProfileSubject(x.Identity),
                new QualityNameOutcomeData(x.QualityName)
            ),
            QualityProfileQualitiesRequiredOutcome x => C.QualityProfileQualitiesRequired.Create(
                $"Quality profile {Profile(x.Identity)}: `qualities` is required when creating "
                    + "a profile",
                ProfileSubject(x.Identity)
            ),
            QualityProfileQualityReferenceMismatchOutcome x =>
                C.QualityProfileQualityReferenceMismatch.Create(
                    $"Quality profile {Profile(x.Identity)} references qualities the service does "
                        + $"not have: {Names(x.Names)}",
                    ProfileSubject(x.Identity),
                    new QualityNamesOutcomeData(x.Names)
                ),
            QualityProfileResetScoreReferenceMismatchOutcome x =>
                C.QualityProfileResetScoreReferenceMismatch.Create(
                    ResetMessage(x),
                    ProfileSubject(x.Identity),
                    new ResetScoreReferencesOutcomeData(x.Names, x.Patterns)
                ),
            QualityProfileRenameBlockedOutcome x => C.QualityProfileRenameBlocked.Create(
                $"Quality profile {Profile(x.Identity)} cannot be renamed to "
                    + $"'{x.Conflict.Name}' because a profile with that name already exists (ID "
                    + $"{x.Conflict.ServiceId}). Delete or rename the existing profile in the "
                    + "service",
                ProfileSubject(x.Identity),
                new RenameBlockedOutcomeData(
                    new NamedServiceResourceResponse(x.Conflict.Name, x.Conflict.ServiceId)
                )
            ),
            QualityProfileAmbiguousMatchOutcome x => C.QualityProfileAmbiguousMatch.Create(
                $"Quality profile {Profile(x.Identity)} cannot sync because multiple service "
                    + $"profiles match its name: {Matches(x.ServiceMatches.Select(y => (y.Name, y.ServiceId)))}. "
                    + "Delete or rename the duplicates in the service",
                ProfileSubject(x.Identity),
                new ServiceMatchesOutcomeData(
                    x.ServiceMatches.Select(y => new NamedServiceResourceResponse(
                            y.Name,
                            y.ServiceId
                        ))
                        .ToList()
                )
            ),
            QualityProfileCreateRejectedOutcome x => C.QualityProfileCreateRejected.Create(
                $"The service rejected creation of quality profile {Profile(x.Identity)}",
                ProfileSubject(x.Identity)
            ),
            QualityProfileUpdateRejectedOutcome x => C.QualityProfileUpdateRejected.Create(
                $"The service rejected update of quality profile {Profile(x.Identity)}",
                ProfileSubject(x.Identity)
            ),
            QualityProfileQualitySizeOutcome x => Map(x.Outcome, x.Identity),
            QualityProfileQualityDefinitionIgnoredOutcome x =>
                C.QualityProfileQualityDefinitionIgnored.Create(
                    $"Quality profile {Profile(x.Identity)}: this service version does not store "
                        + "quality sizes per profile, so the profile's `quality_definition` is "
                        + "ignored",
                    ProfileSubject(x.Identity)
                ),
            QualityProfileQualityDefinitionUnusedOutcome =>
                C.QualityProfileQualityDefinitionUnused.Create(
                    "This service version stores quality sizes per quality profile. "
                        + "`quality_definition` only applies to profiles listed under "
                        + "`quality_profiles`, and none are configured"
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    /// <summary>
    /// Maps a quality size outcome. <paramref name="profile"/> is set when the outcome belongs to
    /// one quality profile on a service that stores sizes per profile.
    /// </summary>
    public static OutcomeResponse Map(
        QualitySizeOutcome outcome,
        QualityProfileIdentity? profile = null
    )
    {
        var prefix = profile is null ? "" : $"Quality profile {Profile(profile)}: ";
        var subject = profile is null ? null : ProfileSubject(profile);

        return outcome switch
        {
            QualitySizeDefinitionReferenceMismatchOutcome x =>
                C.QualitySizeDefinitionReferenceMismatch.Create(
                    $"{prefix}Quality definition type '{x.Type}' does not exist in the guide",
                    subject,
                    new QualityDefinitionTypeOutcomeData(x.Type)
                ),
            QualitySizeReferenceMismatchOutcome x => C.QualitySizeReferenceMismatch.Create(
                $"{prefix}Quality '{x.Quality}' does not exist in the guide for type '{x.Type}'",
                subject,
                new QualitySizeReferenceOutcomeData(x.Quality, x.Type)
            ),
            QualitySizeServiceQualityNotFoundOutcome x =>
                C.QualitySizeServiceQualityNotFound.Create(
                    $"{prefix}The service has no quality '{x.Quality}', so its size is not set",
                    subject,
                    new QualityNameOutcomeData(x.Quality)
                ),
            QualitySizePreferredRatioClampedOutcome x => C.QualitySizePreferredRatioClamped.Create(
                $"{prefix}`preferred_ratio` of {Number(x.Configured)} is out of range "
                    + $"(0.0-1.0), clamped to {Number(x.Used)}",
                subject,
                new PreferredRatioClampedOutcomeData(x.Configured, x.Used)
            ),
            QualitySizeMinimumGreaterThanPreferredOutcome x =>
                C.QualitySizeMinimumGreaterThanPreferred.Create(
                    $"{prefix}Quality '{x.Quality}': min ({x.Minimum}) cannot be greater than "
                        + $"preferred ({x.Preferred})",
                    subject,
                    new MinimumComparisonOutcomeData(
                        x.Quality,
                        ResultValueMapper.MapQualitySize(x.Minimum),
                        ResultValueMapper.MapQualitySize(x.Preferred)
                    )
                ),
            QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome x =>
                C.QualitySizeUnlimitedPreferredGreaterThanMaximum.Create(
                    $"{prefix}Quality '{x.Quality}': preferred (unlimited) cannot be greater than "
                        + $"max ({x.Maximum})",
                    subject,
                    MaximumComparison(x.Quality, x.Preferred, x.Maximum)
                ),
            QualitySizePreferredGreaterThanMaximumOutcome x =>
                C.QualitySizePreferredGreaterThanMaximum.Create(
                    $"{prefix}Quality '{x.Quality}': preferred ({x.Preferred}) cannot be greater "
                        + $"than max ({x.Maximum})",
                    subject,
                    MaximumComparison(x.Quality, x.Preferred, x.Maximum)
                ),
            QualitySizeGuideReferenceMismatchOutcome x =>
                C.QualitySizeGuideReferenceMismatch.Create(
                    $"{prefix}The guide profile references a quality definition that does not "
                        + $"exist: {x.Label} ({x.TrashId})",
                    subject,
                    new QualitySizeGuideReferenceOutcomeData(x.Label, x.TrashId)
                ),
            QualitySizeAmbiguousGuideReferenceOutcome x =>
                C.QualitySizeAmbiguousGuideReference.Create(
                    $"{prefix}The guide profile references more than one quality definition: "
                        + string.Join(", ", x.Labels),
                    subject,
                    new QualitySizeGuideLabelsOutcomeData(x.Labels)
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };
    }

    public static OutcomeResponse Map(SonarrNamingOutcome outcome) =>
        outcome switch
        {
            SonarrNamingReferenceMismatchOutcome x => NamingMismatch(
                x.Field.ToString(),
                x.ConfiguredKey
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    public static OutcomeResponse Map(RadarrNamingOutcome outcome) =>
        outcome switch
        {
            RadarrNamingReferenceMismatchOutcome x => NamingMismatch(
                x.Field.ToString(),
                x.ConfiguredKey
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static OutcomeResponse NamingMismatch(string field, string configuredKey)
    {
        var wireField = JsonNamingPolicy.CamelCase.ConvertName(field);
        return C.MediaNamingReferenceMismatch.Create(
            $"Media naming `{wireField}` references unknown format '{configuredKey}'",
            null,
            new MediaNamingReferenceOutcomeData(wireField, configuredKey)
        );
    }

    private static string ResetMessage(QualityProfileResetScoreReferenceMismatchOutcome x)
    {
        var parts = new List<string>();
        if (x.Names.Count > 0)
        {
            parts.Add($"`except` names not in the profile: {Names(x.Names)}");
        }

        if (x.Patterns.Count > 0)
        {
            parts.Add($"`except_patterns` matching no custom format: {Names(x.Patterns)}");
        }

        return $"Quality profile {Profile(x.Identity)}: `reset_unmatched_scores` has "
            + string.Join("; ", parts);
    }

    private static MaximumComparisonOutcomeData MaximumComparison(
        string quality,
        QualitySizeValue preferred,
        QualitySizeValue maximum
    ) =>
        new(
            quality,
            ResultValueMapper.MapQualitySize(preferred),
            ResultValueMapper.MapQualitySize(maximum)
        );

    private static OutcomeSubjectResponse CustomFormat(string trashId) =>
        new() { Kind = CustomFormatKind, TrashId = trashId };

    private static OutcomeSubjectResponse CustomFormat(CustomFormatIdentity identity) =>
        new()
        {
            Kind = CustomFormatKind,
            TrashId = identity.TrashId,
            Name = identity.Name,
        };

    private static OutcomeSubjectResponse Group(string trashId, string? name = null) =>
        new()
        {
            Kind = CustomFormatGroupKind,
            TrashId = trashId,
            Name = name,
        };

    private static string Profile(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x =>
                $"'{x.MappingKey.Name}' ({x.MappingKey.TrashId})",
            UserDefinedQualityProfileIdentity x => $"'{x.Name}'",
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

    private static OutcomeSubjectResponse ProfileSubject(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x => new OutcomeSubjectResponse
            {
                Kind = QualityProfileKind,
                TrashId = x.MappingKey.TrashId,
                Name = x.MappingKey.Name,
            },
            UserDefinedQualityProfileIdentity x => new OutcomeSubjectResponse
            {
                Kind = QualityProfileKind,
                Name = x.Name,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

    private static string Identity(CustomFormatIdentity identity) =>
        $"'{identity.Name}' ({identity.TrashId})";

    private static string Names(IEnumerable<string> names) =>
        string.Join(", ", names.Select(x => $"'{x}'"));

    private static string Matches(IEnumerable<(string Name, int ServiceId)> matches) =>
        string.Join(", ", matches.Select(x => $"'{x.Name}' (ID {x.ServiceId})"));

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
