namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlanBuilder(IOrderedEnumerable<IPlanComponent> components, ILogger log)
{
    public PipelinePlan Build()
    {
        var plan = new PipelinePlan();

        foreach (var component in components)
        {
            log.Debug("Running plan component: {Component}", component.GetType().Name);
            component.Process(plan);
        }

        return plan;
    }
}
