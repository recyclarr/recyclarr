using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Pipelines.Plan;

/// <summary>
/// The custom format pipeline's plan: one entry per distinct trash_id. Quality profile planning
/// reads it to build scores, and the entries are shared with <see cref="PlannedCfScore"/> so IDs
/// assigned during custom format persistence reach quality profile sync.
/// </summary>
internal sealed record PlannedCustomFormats(IReadOnlyCollection<PlannedCustomFormat> Items);

internal class PlannedCustomFormat(CustomFormatResource resource)
{
    public CustomFormatResource Resource { get; } = resource;
    public ICollection<AssignScoresToConfig> AssignScoresTo { get; init; } = [];
    public string? GroupName { get; init; }
    public CfSource Source { get; init; } = CfSource.FlatConfig;
    public CfInclusionReason InclusionReason { get; init; } = CfInclusionReason.None;
}
