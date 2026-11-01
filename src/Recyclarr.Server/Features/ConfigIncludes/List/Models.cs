using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.ConfigIncludes.List;

internal sealed record ListConfigIncludesResponse(
    IReadOnlyList<ConfigIncludeSummaryResponse> Items
);

internal sealed record ConfigIncludeSummaryResponse
{
    /// <summary>The id to use with `template:` under `include:` in a configuration file.</summary>
    public required string Id { get; init; }

    public required SupportedServices Service { get; init; }
}
