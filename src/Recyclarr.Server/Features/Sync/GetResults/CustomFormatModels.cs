using System.ComponentModel;

namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>Where in the configuration the custom format was selected.</summary>
internal enum CustomFormatSource
{
    [Description("The `custom_formats` list.")]
    FlatConfig,

    [Description("The format items of a guide quality profile.")]
    ProfileFormatItems,

    [Description("A custom format group that a selected guide quality profile includes.")]
    CfGroupImplicit,

    [Description("A group listed in `custom_format_groups.add`.")]
    CfGroupExplicit,
}

/// <summary>Why a group member was included.</summary>
internal enum CustomFormatInclusionReason
{
    [Description("The format did not come from a group.")]
    None,

    [Description("The group always includes the format.")]
    Required,

    [Description("The group includes the format by default.")]
    Default,

    [Description("The configuration selects the format from the group.")]
    Selected,
}

/// <summary>How the configuration selected a custom format.</summary>
internal sealed record CustomFormatSelectionResponse(
    CustomFormatSource Source,
    string? GroupName,
    CustomFormatInclusionReason InclusionReason,
    IReadOnlyList<string> ProfileNames
);

internal enum CustomFormatSpecificationChange
{
    [Description("The sync adds the condition.")]
    Added,

    [Description("The sync changes the condition's settings.")]
    Changed,

    [Description("The sync removes a condition the guide no longer has.")]
    Removed,

    Unchanged,
}

/// <summary>One condition of the custom format, by name.</summary>
internal sealed record CustomFormatSpecificationResponse(
    string Name,
    CustomFormatSpecificationChange Change
);

internal sealed record CustomFormatItemResponse
{
    public required ResourceAction Action { get; init; }
    public required TrashIdNameResponse Identity { get; init; }
    public required CustomFormatSelectionResponse SelectionProvenance { get; init; }
    public required PlannedValueResponse<string> Name { get; init; }
    public required PlannedValueResponse<bool?> IncludeWhenRenaming { get; init; }

    /// <summary>
    /// Every condition of the guide definition, plus conditions the sync removes.
    /// </summary>
    public required IReadOnlyList<CustomFormatSpecificationResponse> Specifications { get; init; }
}

internal sealed record CustomFormatPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }

    /// <summary>
    /// Every configured custom format, changed or not. A format skipped by an outcome is absent.
    /// </summary>
    public required IReadOnlyList<CustomFormatItemResponse> Items { get; init; }

    /// <summary>
    /// Managed custom formats removed from the configuration. Deletes happen only when
    /// `delete_old_custom_formats` is enabled.
    /// </summary>
    public required IReadOnlyList<TrashIdNameResponse> Deletes { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
