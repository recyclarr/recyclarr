using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Models;
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
        IReadOnlyList<QualityProfileDelta> deltas
    )
        : this(
            DeriveStatus(completedResources, incompleteResources),
            null,
            completedResources,
            incompleteResources,
            outcomes,
            deltas
        ) { }

    private QualityProfilePipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<QualityProfileDelta> deltas
    )
        : base(status, blockedBy)
    {
        CompletedResources = completedResources;
        IncompleteResources = incompleteResources;
        Outcomes = outcomes;
        Deltas = deltas;
    }

    public IReadOnlyList<QualityProfileOutcome> Outcomes { get; }
    public IReadOnlyList<QualityProfileDelta> Deltas { get; }
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
            Deltas
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

public abstract record QualityProfileDelta : ResourceDelta
{
    public required QualityProfileIdentity Identity { get; init; }
}

public sealed record QualityProfileCreateDelta : QualityProfileDelta
{
    public required QualityProfileControlledState State { get; init; }
}

public sealed record QualityProfileUpdateDelta : QualityProfileDelta
{
    public required IReadOnlyList<QualityProfileUpdateComponent> Components { get; init; }
}

/// <summary>
/// The profile fields Recyclarr controls when it creates a profile. Fields that only one service
/// has live on that service's derived type (ADR-023).
/// </summary>
public record QualityProfileControlledState
{
    public required string Name { get; init; }
    public required bool? UpgradeAllowed { get; init; }
    public required string? UpgradeUntilQuality { get; init; }
    public required int? UpgradeUntilScore { get; init; }
    public required int? MinimumFormatScore { get; init; }
    public required int? MinimumUpgradeFormatScore { get; init; }
    public required IReadOnlyList<QualityProfileQualityLayoutItem> Qualities { get; init; }
    public required IReadOnlyList<QualityProfileCustomFormatScore> CustomFormatScores { get; init; }
}

public sealed record RadarrQualityProfileControlledState : QualityProfileControlledState
{
    [SetsRequiredMembers]
    public RadarrQualityProfileControlledState(
        QualityProfileControlledState shared,
        string? language
    )
        : base(shared)
    {
        Language = language;
    }

    public string? Language { get; }
}

/// <summary>
/// Controlled state for a Sonarr profile. Only Sonarr v5 stores quality sizes per profile; on
/// earlier versions <see cref="QualitySizes"/> is empty.
/// </summary>
public sealed record SonarrQualityProfileControlledState : QualityProfileControlledState
{
    [SetsRequiredMembers]
    public SonarrQualityProfileControlledState(
        QualityProfileControlledState shared,
        IReadOnlyList<QualityProfileQualitySize> qualitySizes
    )
        : base(shared)
    {
        QualitySizes = qualitySizes;
    }

    public IReadOnlyList<QualityProfileQualitySize> QualitySizes { get; }
}

/// <summary>
/// Size limits Recyclarr assigns to one quality of a profile, in MB per minute.
/// </summary>
public sealed record QualityProfileQualitySize(
    string Quality,
    QualitySizeValue Minimum,
    QualitySizeValue Preferred,
    QualitySizeValue Maximum
);

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

public sealed record QualityProfileCustomFormatScore(string Name, string? TrashId, int Score);

public abstract record QualityProfileUpdateComponent;

public sealed record QualityProfileNameChanged(ValueDelta<string> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileUpgradeAllowedChanged(ValueDelta<bool?> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileUpgradeUntilQualityChanged(ValueDelta<string?> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileUpgradeUntilScoreChanged(ValueDelta<int?> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileMinimumFormatScoreChanged(ValueDelta<int?> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileMinimumUpgradeFormatScoreChanged(ValueDelta<int?> Value)
    : QualityProfileUpdateComponent;

public sealed record RadarrQualityProfileLanguageChanged(ValueDelta<string?> Value)
    : QualityProfileUpdateComponent;

/// <summary>
/// Per-quality size changes on a Sonarr profile. Only Sonarr v5 stores sizes per profile.
/// </summary>
public sealed record SonarrQualityProfileQualitySizesChanged(IReadOnlyList<QualitySizeDelta> Sizes)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileQualityLayoutChanged(
    IReadOnlyList<QualityProfileQualityLayoutItem> Current,
    IReadOnlyList<QualityProfileQualityLayoutItem> Desired
) : QualityProfileUpdateComponent;

public enum QualityProfileScoreChangeReason
{
    Set,
    Reset,
}

public sealed record QualityProfileCustomFormatScoreChanged(
    string Name,
    string? TrashId,
    ValueDelta<int> Value,
    QualityProfileScoreChangeReason Reason
) : QualityProfileUpdateComponent;
