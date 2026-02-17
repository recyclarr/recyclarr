using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class MediaManagementPlanComponent(IServiceConfiguration config) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        var propersAndRepacks = config.MediaManagement.PropersAndRepacks;

        // Only add to plan if explicitly configured
        if (propersAndRepacks is null)
        {
            return;
        }

        plan.MediaManagement = new PlannedMediaManagement(propersAndRepacks.Value);
    }
}
