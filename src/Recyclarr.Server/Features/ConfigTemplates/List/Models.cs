using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.ConfigTemplates.List;

internal sealed record ListConfigTemplatesResponse(
    IReadOnlyList<ConfigTemplateSummaryResponse> Items
);

internal sealed record ConfigTemplateSummaryResponse
{
    /// <summary>
    /// The id to request the template with. It is the guide file name of the quality profile, with a
    /// `radarr-` or `sonarr-` prefix when both services have a profile file of that name.
    /// </summary>
    public required string Id { get; init; }

    public required SupportedServices Service { get; init; }

    /// <summary>The name of the guide quality profile the template syncs.</summary>
    public required string ProfileName { get; init; }

    /// <summary>The trash_id of the guide quality profile the template syncs.</summary>
    public required string ProfileTrashId { get; init; }

    /// <summary>Link to the guide page for the profile; absent when the guide has none.</summary>
    public string? TrashUrl { get; init; }
}
