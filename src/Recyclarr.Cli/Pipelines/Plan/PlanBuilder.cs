using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlanBuilder(
    IOrderedEnumerable<IPlanComponent> components,
    IInstancePublisher publisher,
    ILogger log
)
{
    public PipelinePlan Build()
    {
        var plan = new PipelinePlan(publisher);

        foreach (var component in components)
        {
            log.Debug("Running plan component: {Component}", component.GetType().Name);
            component.Process(plan);
        }

        return plan;
    }
}
