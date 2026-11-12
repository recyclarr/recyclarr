using System.Diagnostics.CodeAnalysis;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// The terminal status, outcomes, and calculated resource changes for profile synchronization.
/// </summary>
public sealed record QualityProfilePipelineResult : PipelineResult
{
    internal QualityProfilePipelineResult(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<PlannedQualityProfileResource> resources
    )
        : this(
            DeriveStatus(completedResources, incompleteResources),
            null,
            completedResources,
            incompleteResources,
            outcomes,
            resources
        ) { }

    private QualityProfilePipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<PlannedQualityProfileResource> resources
    )
        : base(status, blockedBy)
    {
        CompletedResources = completedResources;
        IncompleteResources = incompleteResources;
        Outcomes = outcomes;
        Resources = resources;
    }

    public IReadOnlyList<QualityProfileOutcome> Outcomes { get; }
    public IReadOnlyList<PlannedQualityProfileResource> Resources { get; }
    internal int CompletedResources { get; }
    internal int IncompleteResources { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) =>
        new QualityProfilePipelineResult(
            status,
            blockedBy,
            CompletedResources,
            IncompleteResources,
            Outcomes,
            Resources
        );

    private static SyncResultStatus DeriveStatus(int completedResources, int incompleteResources)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(completedResources);
        ArgumentOutOfRangeException.ThrowIfNegative(incompleteResources);

        if (incompleteResources == 0)
        {
            return SyncResultStatus.Succeeded;
        }

        return completedResources > 0 ? SyncResultStatus.Partial : SyncResultStatus.Failed;
    }
}

public abstract record QualityProfileIdentity;

public sealed record GuideBackedQualityProfileIdentity(MappingKey MappingKey)
    : QualityProfileIdentity;

public sealed record UserDefinedQualityProfileIdentity(string Name) : QualityProfileIdentity;

public abstract record QualityProfileOutcome : PipelineOutcome;

public sealed record QualityProfileReferenceMismatchOutcome(string TrashId) : QualityProfileOutcome;

public sealed record QualityProfileDuplicateNameOutcome(string Name) : QualityProfileOutcome;

public sealed record QualityProfileNotFoundOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

public sealed record QualityProfileAdoptedOutcome(QualityProfileIdentity Identity, int ServiceId)
    : QualityProfileOutcome;

public sealed record QualityProfileMinimumScoreUnsatisfiedOutcome(
    QualityProfileIdentity Identity,
    int MinimumScore,
    int TotalPositiveScore,
    int MaximumScore
) : QualityProfileOutcome;

public sealed record QualityProfileInvalidCutoffOutcome(
    QualityProfileIdentity Identity,
    string QualityName
) : QualityProfileOutcome;

public sealed record QualityProfileUnavailableCutoffOutcome(
    QualityProfileIdentity Identity,
    string QualityName
) : QualityProfileOutcome;

public sealed record QualityProfileQualitiesRequiredOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

public sealed record QualityProfileQualityReferenceMismatchOutcome(
    QualityProfileIdentity Identity,
    IReadOnlyList<string> Names
) : QualityProfileOutcome;

public sealed record QualityProfileResetScoreReferenceMismatchOutcome(
    QualityProfileIdentity Identity,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Patterns
) : QualityProfileOutcome;

public sealed record QualityProfileServiceMatch(string Name, int ServiceId);

public sealed record QualityProfileRenameBlockedOutcome(
    QualityProfileIdentity Identity,
    QualityProfileServiceMatch Conflict
) : QualityProfileOutcome;

public sealed record QualityProfileAmbiguousMatchOutcome(
    QualityProfileIdentity Identity,
    IReadOnlyList<QualityProfileServiceMatch> ServiceMatches
) : QualityProfileOutcome;

public sealed record QualityProfileCreateRejectedOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

public sealed record QualityProfileUpdateRejectedOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

/// <summary>
/// A problem with the quality sizes resolved for one profile on a service that stores sizes per
/// quality profile (Sonarr v5). Qualities named by an error keep the service's sizes; the rest of
/// the profile still syncs.
/// </summary>
public sealed record QualityProfileQualitySizeOutcome(
    QualityProfileIdentity Identity,
    QualitySizeOutcome Outcome
) : QualityProfileOutcome;

/// <summary>
/// The profile's <c>quality_definition</c> was ignored because the service stores quality sizes
/// globally rather than per quality profile.
/// </summary>
public sealed record QualityProfileQualityDefinitionIgnoredOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

/// <summary>
/// The instance <c>quality_definition</c> applies to nothing: the service stores sizes per quality
/// profile and no profile is listed under <c>quality_profiles</c>.
/// </summary>
public sealed record QualityProfileQualityDefinitionUnusedOutcome : QualityProfileOutcome;

/// <summary>
/// One valid configured profile and every value the sync manages on it. Fields that only one
/// service has live on that service's derived type (ADR-023). A profile skipped by an outcome has
/// no entry.
/// </summary>
public record PlannedQualityProfileResource : PlannedResource
{
    public required QualityProfileIdentity Identity { get; init; }
    public required PlannedValue<string> Name { get; init; }
    public required QualityProfileField<bool?> UpgradeAllowed { get; init; }
    public required QualityProfileField<string?> UpgradeUntilQuality { get; init; }
    public required QualityProfileField<int?> UpgradeUntilScore { get; init; }
    public required QualityProfileField<int?> MinimumFormatScore { get; init; }
    public required QualityProfileField<int?> MinimumUpgradeFormatScore { get; init; }

    public required QualityProfileField<
        IReadOnlyList<QualityProfileQualityLayoutItem>
    > Qualities { get; init; }

    /// <summary>Every custom format score in the profile, including scores the sync keeps.</summary>
    public required IReadOnlyList<QualityProfileScoreResult> CustomFormatScores { get; init; }
}

public sealed record RadarrPlannedQualityProfileResource : PlannedQualityProfileResource
{
    [SetsRequiredMembers]
    public RadarrPlannedQualityProfileResource(
        PlannedQualityProfileResource shared,
        QualityProfileField<string?> language
    )
        : base(shared)
    {
        Language = language;
    }

    public QualityProfileField<string?> Language { get; }
}

/// <summary>
/// A Sonarr profile. Only Sonarr v5 stores quality sizes per profile; on earlier versions
/// <see cref="QualitySizes"/> is empty.
/// </summary>
public sealed record SonarrPlannedQualityProfileResource : PlannedQualityProfileResource
{
    [SetsRequiredMembers]
    public SonarrPlannedQualityProfileResource(
        PlannedQualityProfileResource shared,
        IReadOnlyList<PlannedQualitySizeResource> qualitySizes
    )
        : base(shared)
    {
        QualitySizes = qualitySizes;
    }

    /// <summary>The sizes of every planned quality the profile contains.</summary>
    public IReadOnlyList<PlannedQualitySizeResource> QualitySizes { get; }
}

public abstract record QualityProfileQualityLayoutItem
{
    public required string Name { get; init; }
    public required bool Allowed { get; init; }
}

public sealed record QualityProfileQuality : QualityProfileQualityLayoutItem;

public sealed record QualityProfileQualityGroup : QualityProfileQualityLayoutItem
{
    public required IReadOnlyList<string> Qualities { get; init; }
}

/// <summary>
/// Why a profile field has its desired value. A set field is a managed value: configuration, guide
/// data, or a Recyclarr rule decides it. An unmanaged field keeps the service value.
/// </summary>
public enum QualityProfileFieldReason
{
    Set,
    Unmanaged,
}

/// <summary>
/// A profile field whose value may be managed or unmanaged. The reason sits beside the value
/// because a value holds only plan state (ADR-016).
/// </summary>
public sealed record QualityProfileField<T>(
    QualityProfileFieldReason Reason,
    PlannedValue<T> Value
);

public static class QualityProfileField
{
    public static QualityProfileField<T> Of<T>(bool managed, PlannedValue<T> value) =>
        new(managed ? QualityProfileFieldReason.Set : QualityProfileFieldReason.Unmanaged, value);
}

/// <summary>Why a custom format score has its desired value.</summary>
public enum QualityProfileScoreReason
{
    /// <summary>The configuration or guide sets the score.</summary>
    Set,

    /// <summary><c>reset_unmatched_scores</c> sets the score to 0.</summary>
    Reset,

    /// <summary>Nothing sets the score, so it keeps its value in the service.</summary>
    Unmanaged,
}

public sealed record QualityProfileScoreResult(
    string Name,
    string? TrashId,
    PlannedValue<int> Value,
    QualityProfileScoreReason Reason
);
