using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile;

public sealed record QualityProfilePipelineResult : Recyclarr.Sync.Results.PipelineResult
{
    internal QualityProfilePipelineResult(
        int completedResources,
        int incompleteResources,
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<QualityProfileDelta> deltas
    )
        : base(DeriveStatus(completedResources, incompleteResources))
    {
        Outcomes = outcomes.ToList().AsReadOnly();
        Deltas = deltas.ToList().AsReadOnly();
    }

    public IReadOnlyList<QualityProfileOutcome> Outcomes { get; }
    public IReadOnlyList<QualityProfileDelta> Deltas { get; }

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

public sealed record QualityProfileCustomFormatReference(string Name, string TrashId);

public sealed record QualityProfileScoreCollisionOutcome(
    QualityProfileCustomFormatReference Existing,
    QualityProfileCustomFormatReference Rejected,
    int ServiceId
) : QualityProfileOutcome;

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

public sealed record QualityProfileQualityReferenceMismatchOutcome : QualityProfileOutcome
{
    public QualityProfileQualityReferenceMismatchOutcome(
        QualityProfileIdentity identity,
        IReadOnlyList<string> names
    )
    {
        Identity = identity;
        Names = names.ToList().AsReadOnly();
    }

    public QualityProfileIdentity Identity { get; }
    public IReadOnlyList<string> Names { get; }
}

public sealed record QualityProfileResetScoreReferenceMismatchOutcome : QualityProfileOutcome
{
    public QualityProfileResetScoreReferenceMismatchOutcome(
        QualityProfileIdentity identity,
        IReadOnlyList<string> names,
        IReadOnlyList<string> patterns
    )
    {
        Identity = identity;
        Names = names.ToList().AsReadOnly();
        Patterns = patterns.ToList().AsReadOnly();
    }

    public QualityProfileIdentity Identity { get; }
    public IReadOnlyList<string> Names { get; }
    public IReadOnlyList<string> Patterns { get; }
}

public sealed record QualityProfileServiceMatch(string Name, int ServiceId);

public sealed record QualityProfileRenameBlockedOutcome(
    QualityProfileIdentity Identity,
    QualityProfileServiceMatch Conflict
) : QualityProfileOutcome;

public sealed record QualityProfileAmbiguousMatchOutcome : QualityProfileOutcome
{
    public QualityProfileAmbiguousMatchOutcome(
        QualityProfileIdentity identity,
        IReadOnlyList<QualityProfileServiceMatch> serviceMatches
    )
    {
        Identity = identity;
        ServiceMatches = serviceMatches.ToList().AsReadOnly();
    }

    public QualityProfileIdentity Identity { get; }
    public IReadOnlyList<QualityProfileServiceMatch> ServiceMatches { get; }
}

public sealed record QualityProfileCreateRejectedOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

public sealed record QualityProfileUpdateRejectedOutcome(QualityProfileIdentity Identity)
    : QualityProfileOutcome;

public abstract record QualityProfileDelta(QualityProfileIdentity Identity) : ResourceDelta;

public sealed record QualityProfileCreateDelta(
    QualityProfileIdentity Identity,
    QualityProfileControlledState State
) : QualityProfileDelta(Identity);

public sealed record QualityProfileUpdateDelta : QualityProfileDelta
{
    public QualityProfileUpdateDelta(
        QualityProfileIdentity identity,
        IReadOnlyList<QualityProfileUpdateComponent> components
    )
        : base(identity)
    {
        Components = components.ToList().AsReadOnly();
    }

    public IReadOnlyList<QualityProfileUpdateComponent> Components { get; }
}

public sealed record QualityProfileControlledState
{
    public QualityProfileControlledState(
        string name,
        bool? upgradeAllowed,
        string? upgradeUntilQuality,
        int? upgradeUntilScore,
        int? minimumFormatScore,
        int? minimumUpgradeFormatScore,
        string? language,
        IReadOnlyList<QualityProfileQualityLayoutItem> qualities,
        IReadOnlyList<QualityProfileCustomFormatScore> customFormatScores
    )
    {
        Name = name;
        UpgradeAllowed = upgradeAllowed;
        UpgradeUntilQuality = upgradeUntilQuality;
        UpgradeUntilScore = upgradeUntilScore;
        MinimumFormatScore = minimumFormatScore;
        MinimumUpgradeFormatScore = minimumUpgradeFormatScore;
        Language = language;
        Qualities = qualities.ToList().AsReadOnly();
        CustomFormatScores = customFormatScores.ToList().AsReadOnly();
    }

    public string Name { get; }
    public bool? UpgradeAllowed { get; }
    public string? UpgradeUntilQuality { get; }
    public int? UpgradeUntilScore { get; }
    public int? MinimumFormatScore { get; }
    public int? MinimumUpgradeFormatScore { get; }
    public string? Language { get; }
    public IReadOnlyList<QualityProfileQualityLayoutItem> Qualities { get; }
    public IReadOnlyList<QualityProfileCustomFormatScore> CustomFormatScores { get; }
}

public abstract record QualityProfileQualityLayoutItem(string Name, bool Allowed);

public sealed record QualityProfileQuality(string Name, bool Allowed)
    : QualityProfileQualityLayoutItem(Name, Allowed);

public sealed record QualityProfileQualityGroup : QualityProfileQualityLayoutItem
{
    public QualityProfileQualityGroup(string name, bool allowed, IReadOnlyList<string> qualities)
        : base(name, allowed)
    {
        Qualities = qualities.ToList().AsReadOnly();
    }

    public IReadOnlyList<string> Qualities { get; }
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

public sealed record QualityProfileLanguageChanged(ValueDelta<string?> Value)
    : QualityProfileUpdateComponent;

public sealed record QualityProfileQualityLayoutChanged : QualityProfileUpdateComponent
{
    public QualityProfileQualityLayoutChanged(
        IReadOnlyList<QualityProfileQualityLayoutItem> current,
        IReadOnlyList<QualityProfileQualityLayoutItem> desired
    )
    {
        Current = current.ToList().AsReadOnly();
        Desired = desired.ToList().AsReadOnly();
    }

    public IReadOnlyList<QualityProfileQualityLayoutItem> Current { get; }
    public IReadOnlyList<QualityProfileQualityLayoutItem> Desired { get; }
}

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
