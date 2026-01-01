using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal record ConfiguredCfEntry(
    string TrashId,
    ICollection<AssignScoresToConfig> AssignScoresTo,
    string? GroupName
);
