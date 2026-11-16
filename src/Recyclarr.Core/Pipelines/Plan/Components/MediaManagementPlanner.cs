using Recyclarr.Config.Models;

namespace Recyclarr.Pipelines.Plan.Components;

internal class MediaManagementPlanner(IServiceConfiguration config)
    : IPipelinePlanner<PlannedMediaManagement>
{
    public PlannedMediaManagement? Plan(PlanningContext context)
    {
        var propersAndRepacks = config.MediaManagement.PropersAndRepacks;

        // Only plan if explicitly configured
        return propersAndRepacks is null
            ? null
            : new PlannedMediaManagement(propersAndRepacks.Value);
    }
}
