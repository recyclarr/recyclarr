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

internal sealed record CustomFormatCreateResponse(
    TrashIdNameResponse Identity,
    CustomFormatSelectionResponse SelectionProvenance
);

internal sealed record CustomFormatUpdateResponse
{
    public required TrashIdNameResponse Identity { get; init; }
    public required CustomFormatSelectionResponse SelectionProvenance { get; init; }
    public ValueChangeResponse<string>? Name { get; init; }
    public ValueChangeResponse<bool>? IncludeWhenRenaming { get; init; }

    /// <summary>Names of the conditions added to the custom format.</summary>
    public IReadOnlyList<string> SpecificationsAdded { get; init; } = [];

    /// <summary>Names of the conditions changed in the custom format.</summary>
    public IReadOnlyList<string> SpecificationsChanged { get; init; } = [];

    /// <summary>Names of the conditions removed from the custom format.</summary>
    public IReadOnlyList<string> SpecificationsRemoved { get; init; } = [];
}

internal sealed record CustomFormatPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }
    public required IReadOnlyList<CustomFormatCreateResponse> Creates { get; init; }
    public required IReadOnlyList<CustomFormatUpdateResponse> Updates { get; init; }

    /// <summary>
    /// Managed custom formats removed from the configuration. Deletes happen only when
    /// `delete_old_custom_formats` is enabled.
    /// </summary>
    public required IReadOnlyList<TrashIdNameResponse> Deletes { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
