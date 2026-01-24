using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal enum CfSource
{
    FlatConfig,
    ProfileFormatItems,
    CfGroupImplicit,
    CfGroupExplicit,
}

internal enum CfInclusionReason
{
    None,
    Required,
    Default,
    Selected,
}

internal record ConfiguredCfEntry(
    string TrashId,
    ICollection<AssignScoresToConfig> AssignScoresTo,
    string? GroupName,
    CfSource Source = CfSource.FlatConfig,
    CfInclusionReason InclusionReason = CfInclusionReason.None
);
