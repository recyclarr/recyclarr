using Recyclarr.Config.Models;

namespace Recyclarr.Pipelines.CustomFormat;

internal record ConfiguredCfEntry(
    string TrashId,
    ICollection<AssignScoresToConfig> AssignScoresTo,
    string? GroupName,
    CfSource Source = CfSource.FlatConfig,
    CfInclusionReason InclusionReason = CfInclusionReason.None
);
