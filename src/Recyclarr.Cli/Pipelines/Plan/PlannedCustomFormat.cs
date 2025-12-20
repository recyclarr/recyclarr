using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlannedCustomFormat(CustomFormatResource resource)
{
    public CustomFormatResource Resource { get; } = resource;
    public ICollection<AssignScoresToConfig> AssignScoresTo { get; init; } = [];
}
