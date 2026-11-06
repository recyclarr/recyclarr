using System.Reflection;
using Recyclarr.Server.Features.Sync.GetResults;
using static Recyclarr.Server.Features.Sync.GetResults.OutcomeSeverity;

namespace Recyclarr.Server.Sync.Results;

/// <summary>
/// An outcome code as published in the API: its wire value, the severity and blocking flag every
/// outcome with this code carries, and the type of its data.
/// </summary>
/// <remarks>
/// A published code is a contract. Adding a code is additive; removing one, or renaming a field of
/// its data, needs a new API version (ADR-016).
/// </remarks>
internal interface IOutcomeCode
{
    string Value { get; }
    Type? DataType { get; }
}

/// <summary>A code whose outcomes carry no data.</summary>
internal sealed class OutcomeCode(string value, OutcomeSeverity severity, bool blocking = false)
    : IOutcomeCode
{
    public string Value => value;
    public Type? DataType => null;

    public OutcomeResponse Create(string message, OutcomeSubjectResponse? subject = null) =>
        new()
        {
            Code = value,
            Severity = severity,
            Blocking = blocking,
            Message = message,
            Subject = subject,
        };
}

/// <summary>A code whose outcomes always carry data of type <typeparamref name="TData"/>.</summary>
internal sealed class OutcomeCode<TData>(
    string value,
    OutcomeSeverity severity,
    bool blocking = false
) : IOutcomeCode
    where TData : class
{
    public string Value => value;
    public Type? DataType { get; } = typeof(TData);

    public OutcomeResponse Create(string message, OutcomeSubjectResponse? subject, TData data) =>
        new()
        {
            Code = value,
            Severity = severity,
            Blocking = blocking,
            Message = message,
            Subject = subject,
            Data = data,
        };
}

/// <summary>
/// Every outcome code the API publishes. The OpenAPI document reads the public static fields of
/// this class to map codes to data schemas, so each code must be declared here.
/// </summary>
/// <remarks>
/// Wire values are string literals on purpose, not derived from field names with <c>nameof</c>.
/// A derived value would let a C# rename silently change the published code and break clients,
/// and the committed OpenAPI document lists only codes that have data, so it would not show the
/// change. A literal makes a wire change an explicit edit.
/// </remarks>
internal static class OutcomeCodes
{
    // Planning: configuration problems found before any pipeline runs.
    public static readonly OutcomeCode CustomFormatReferenceMismatch = new(
        "customFormatReferenceMismatch",
        Error,
        blocking: true
    );

    public static readonly OutcomeCode CustomFormatGroupReferenceMismatch = new(
        "customFormatGroupReferenceMismatch",
        Error,
        blocking: true
    );

    public static readonly OutcomeCode CustomFormatGroupSkipReferenceMismatch = new(
        "customFormatGroupSkipReferenceMismatch",
        Error,
        blocking: true
    );

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupSelectReferenceMismatch =
        new("customFormatGroupSelectReferenceMismatch", Error, blocking: true);

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupExcludeReferenceMismatch =
        new("customFormatGroupExcludeReferenceMismatch", Error, blocking: true);

    public static readonly OutcomeCode<QualityProfileReferenceOutcomeData> CustomFormatGroupQualityProfileReferenceMismatch =
        new("customFormatGroupQualityProfileReferenceMismatch", Error, blocking: true);

    public static readonly OutcomeCode<ProfileNamesOutcomeData> CustomFormatQualityProfileReferenceAmbiguous =
        new("customFormatQualityProfileReferenceAmbiguous", Error, blocking: true);

    public static readonly OutcomeCode<QualityProfileReferenceAmbiguousOutcomeData> CustomFormatGroupQualityProfileReferenceAmbiguous =
        new("customFormatGroupQualityProfileReferenceAmbiguous", Error, blocking: true);

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupRequiredItemSelected =
        new("customFormatGroupRequiredItemSelected", Info);

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupDefaultItemSelected =
        new("customFormatGroupDefaultItemSelected", Info);

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupRequiredItemExcluded =
        new("customFormatGroupRequiredItemExcluded", Warning);

    public static readonly OutcomeCode<CustomFormatGroupMemberOutcomeData> CustomFormatGroupNonDefaultItemExcluded =
        new("customFormatGroupNonDefaultItemExcluded", Warning);

    // Custom formats
    public static readonly OutcomeCode CustomFormatGroupIncompatible = new(
        "customFormatGroupIncompatible",
        Warning
    );

    public static readonly OutcomeCode CustomFormatGroupEmpty = new(
        "customFormatGroupEmpty",
        Warning
    );

    public static readonly OutcomeCode<CustomFormatDuplicateNameOutcomeData> CustomFormatDuplicateName =
        new("customFormatDuplicateName", Error);

    public static readonly OutcomeCode<ServiceIdOutcomeData> CustomFormatAdopted = new(
        "customFormatAdopted",
        Info
    );

    public static readonly OutcomeCode<ServiceMatchesOutcomeData> CustomFormatAmbiguousMatch = new(
        "customFormatAmbiguousMatch",
        Error
    );

    public static readonly OutcomeCode<CustomFormatStateConflictOutcomeData> CustomFormatStateConflict =
        new("customFormatStateConflict", Error);

    public static readonly OutcomeCode CustomFormatCreateRejected = new(
        "customFormatCreateRejected",
        Error
    );

    public static readonly OutcomeCode CustomFormatUpdateRejected = new(
        "customFormatUpdateRejected",
        Error
    );

    public static readonly OutcomeCode CustomFormatDeleteRejected = new(
        "customFormatDeleteRejected",
        Error
    );

    // Quality profiles
    public static readonly OutcomeCode QualityProfileReferenceMismatch = new(
        "qualityProfileReferenceMismatch",
        Error
    );

    public static readonly OutcomeCode QualityProfileDuplicateName = new(
        "qualityProfileDuplicateName",
        Error
    );

    public static readonly OutcomeCode QualityProfileNotFound = new(
        "qualityProfileNotFound",
        Error
    );

    public static readonly OutcomeCode<ServiceIdOutcomeData> QualityProfileAdopted = new(
        "qualityProfileAdopted",
        Info
    );

    public static readonly OutcomeCode<MinimumScoreOutcomeData> QualityProfileMinimumScoreUnsatisfied =
        new("qualityProfileMinimumScoreUnsatisfied", Error);

    public static readonly OutcomeCode<QualityNameOutcomeData> QualityProfileInvalidCutoff = new(
        "qualityProfileInvalidCutoff",
        Error
    );

    public static readonly OutcomeCode<QualityNameOutcomeData> QualityProfileUnavailableCutoff =
        new("qualityProfileUnavailableCutoff", Error);

    public static readonly OutcomeCode QualityProfileQualitiesRequired = new(
        "qualityProfileQualitiesRequired",
        Error
    );

    public static readonly OutcomeCode<QualityNamesOutcomeData> QualityProfileQualityReferenceMismatch =
        new("qualityProfileQualityReferenceMismatch", Error);

    public static readonly OutcomeCode<ResetScoreReferencesOutcomeData> QualityProfileResetScoreReferenceMismatch =
        new("qualityProfileResetScoreReferenceMismatch", Error);

    public static readonly OutcomeCode<RenameBlockedOutcomeData> QualityProfileRenameBlocked = new(
        "qualityProfileRenameBlocked",
        Error
    );

    public static readonly OutcomeCode<ServiceMatchesOutcomeData> QualityProfileAmbiguousMatch =
        new("qualityProfileAmbiguousMatch", Error);

    public static readonly OutcomeCode QualityProfileCreateRejected = new(
        "qualityProfileCreateRejected",
        Error
    );

    public static readonly OutcomeCode QualityProfileUpdateRejected = new(
        "qualityProfileUpdateRejected",
        Error
    );

    public static readonly OutcomeCode QualityProfileQualityDefinitionIgnored = new(
        "qualityProfileQualityDefinitionIgnored",
        Warning
    );

    public static readonly OutcomeCode QualityProfileQualityDefinitionUnused = new(
        "qualityProfileQualityDefinitionUnused",
        Warning
    );

    // Quality sizes, reported by the quality size pipeline and, on services that store sizes per
    // quality profile, under each affected quality profile.
    public static readonly OutcomeCode<QualityDefinitionTypeOutcomeData> QualitySizeDefinitionReferenceMismatch =
        new("qualitySizeDefinitionReferenceMismatch", Error);

    public static readonly OutcomeCode<QualitySizeReferenceOutcomeData> QualitySizeReferenceMismatch =
        new("qualitySizeReferenceMismatch", Error);

    public static readonly OutcomeCode<QualityNameOutcomeData> QualitySizeServiceQualityNotFound =
        new("qualitySizeServiceQualityNotFound", Warning);

    public static readonly OutcomeCode<PreferredRatioClampedOutcomeData> QualitySizePreferredRatioClamped =
        new("qualitySizePreferredRatioClamped", Warning);

    public static readonly OutcomeCode<MinimumComparisonOutcomeData> QualitySizeMinimumGreaterThanPreferred =
        new("qualitySizeMinimumGreaterThanPreferred", Error);

    public static readonly OutcomeCode<MaximumComparisonOutcomeData> QualitySizeUnlimitedPreferredGreaterThanMaximum =
        new("qualitySizeUnlimitedPreferredGreaterThanMaximum", Error);

    public static readonly OutcomeCode<MaximumComparisonOutcomeData> QualitySizePreferredGreaterThanMaximum =
        new("qualitySizePreferredGreaterThanMaximum", Error);

    public static readonly OutcomeCode<QualitySizeGuideReferenceOutcomeData> QualitySizeGuideReferenceMismatch =
        new("qualitySizeGuideReferenceMismatch", Error);

    public static readonly OutcomeCode<QualitySizeGuideLabelsOutcomeData> QualitySizeAmbiguousGuideReference =
        new("qualitySizeAmbiguousGuideReference", Error);

    // Media naming
    public static readonly OutcomeCode<MediaNamingReferenceOutcomeData> MediaNamingReferenceMismatch =
        new("mediaNamingReferenceMismatch", Error);

    public static IEnumerable<IOutcomeCode> All =>
        typeof(OutcomeCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(x => x.GetValue(null))
            .OfType<IOutcomeCode>();
}
