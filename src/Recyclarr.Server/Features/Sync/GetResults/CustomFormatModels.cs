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

/// <summary>
/// `identity` is the stale stored entry; `managedIdentity` is the custom format that uses
/// `serviceId` now.
/// </summary>
internal sealed record CustomFormatStateConflictResponse(
    TrashIdNameResponse Identity,
    TrashIdNameResponse ManagedIdentity,
    int ServiceId
);

internal sealed record CustomFormatAmbiguousMatchResponse(
    TrashIdNameResponse Identity,
    IReadOnlyList<NamedServiceResourceResponse> ServiceMatches
);

internal sealed record CustomFormatDuplicateNameResponse(
    string Name,
    IReadOnlyList<string> TrashIds
);

internal sealed record CustomFormatOutcomesResponse
{
    /// <summary>
    /// Configured trash IDs that match no guide custom format; they are not synced.
    /// </summary>
    public IReadOnlyList<string> ReferenceMismatches { get; init; } = [];

    /// <summary>Configured custom format group trash IDs that match no guide group.</summary>
    public IReadOnlyList<string> GroupReferenceMismatches { get; init; } = [];

    /// <summary>Groups that apply to none of the selected quality profiles.</summary>
    public IReadOnlyList<TrashIdNameResponse> IncompatibleGroups { get; init; } = [];

    /// <summary>Groups that contribute no custom formats.</summary>
    public IReadOnlyList<TrashIdNameResponse> EmptyGroups { get; init; } = [];

    /// <summary>
    /// Names shared by more than one configured custom format (ignoring case). Services require
    /// unique names, so none of these custom formats are synced.
    /// </summary>
    public IReadOnlyList<CustomFormatDuplicateNameResponse> DuplicateNames { get; init; } = [];

    /// <summary>
    /// Custom formats that already existed in the service with the same name. Recyclarr now
    /// manages them.
    /// </summary>
    public IReadOnlyList<NamedIdentityResponse> Adopted { get; init; } = [];

    /// <summary>
    /// Custom formats whose name matches more than one service custom format; they are not synced.
    /// </summary>
    public IReadOnlyList<CustomFormatAmbiguousMatchResponse> AmbiguousMatches { get; init; } = [];

    /// <summary>
    /// Stored sync state that maps a custom format to a service ID that another managed custom
    /// format uses.
    /// </summary>
    public IReadOnlyList<CustomFormatStateConflictResponse> StateConflicts { get; init; } = [];

    /// <summary>Custom formats the service refused to create.</summary>
    public IReadOnlyList<TrashIdNameResponse> CreateRejected { get; init; } = [];

    /// <summary>Custom formats the service refused to update.</summary>
    public IReadOnlyList<TrashIdNameResponse> UpdateRejected { get; init; } = [];

    /// <summary>Custom formats the service refused to delete.</summary>
    public IReadOnlyList<TrashIdNameResponse> DeleteRejected { get; init; } = [];
}

internal sealed record NamedIdentityResponse(TrashIdNameResponse Identity, int ServiceId);

internal sealed record CustomFormatPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required CustomFormatOutcomesResponse Outcomes { get; init; }
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
