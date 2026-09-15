namespace Recyclarr.Sync.Results;

/// <summary>
/// The configured Custom Format group does not exist in the active guide resources.
/// </summary>
public sealed record CustomFormatGroupReferenceMismatchPlanningOutcome(string GroupTrashId)
    : BlockingPlanningOutcome;

/// <summary>
/// A Custom Format selected from a group does not exist in that group's guide definition.
/// </summary>
public sealed record CustomFormatGroupSelectReferenceMismatchPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : BlockingPlanningOutcome;

/// <summary>
/// A Custom Format excluded from a group does not exist in that group's guide definition.
/// </summary>
public sealed record CustomFormatGroupExcludeReferenceMismatchPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : BlockingPlanningOutcome;

/// <summary>
/// A Quality Profile assigned to a Custom Format group does not exist in the active guide
/// resources.
/// </summary>
public sealed record CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcome(
    string GroupTrashId,
    string ProfileTrashId
) : BlockingPlanningOutcome;

/// <summary>
/// A Quality Profile Trash ID assigned directly from Custom Format configuration matches more than
/// one configured profile.
/// </summary>
public sealed record CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome
    : BlockingPlanningOutcome
{
    public CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome(
        string profileTrashId,
        IReadOnlyList<string> profileNames
    )
    {
        ArgumentNullException.ThrowIfNull(profileNames);

        ProfileTrashId = profileTrashId;
        ProfileNames = profileNames.ToList().AsReadOnly();
    }

    public string ProfileTrashId { get; }
    public IReadOnlyList<string> ProfileNames { get; }
}

/// <summary>
/// A Quality Profile Trash ID assigned from a Custom Format group matches more than one configured
/// profile.
/// </summary>
public sealed record CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome
    : BlockingPlanningOutcome
{
    public CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome(
        string groupTrashId,
        string profileTrashId,
        IReadOnlyList<string> profileNames
    )
    {
        ArgumentNullException.ThrowIfNull(profileNames);

        GroupTrashId = groupTrashId;
        ProfileTrashId = profileTrashId;
        ProfileNames = profileNames.ToList().AsReadOnly();
    }

    public string GroupTrashId { get; }
    public string ProfileTrashId { get; }
    public IReadOnlyList<string> ProfileNames { get; }
}

/// <summary>
/// A required Custom Format was explicitly selected even though groups always include it.
/// </summary>
public sealed record CustomFormatGroupRequiredItemSelectedPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcome;

/// <summary>
/// A default Custom Format was explicitly selected even though groups include it by default.
/// </summary>
public sealed record CustomFormatGroupDefaultItemSelectedPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcome;

/// <summary>
/// A required Custom Format was excluded even though required group members cannot be excluded.
/// </summary>
public sealed record CustomFormatGroupRequiredItemExcludedPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcome;

/// <summary>
/// A non-default Custom Format was excluded without selecting all optional group members, so the
/// exclusion has no effect.
/// </summary>
public sealed record CustomFormatGroupNonDefaultItemExcludedPlanningOutcome(
    string GroupTrashId,
    string CustomFormatTrashId
) : PlanningOutcome;
