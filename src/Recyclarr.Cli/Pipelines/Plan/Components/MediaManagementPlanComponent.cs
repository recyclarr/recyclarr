using Recyclarr.Config.Models;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class MediaManagementPlanComponent(IServiceConfiguration config) : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
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
