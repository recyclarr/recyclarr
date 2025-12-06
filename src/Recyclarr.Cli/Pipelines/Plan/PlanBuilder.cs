namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlanBuilder(IOrderedEnumerable<IPlanComponent> components, ILogger log)
{
    public (PipelinePlan Plan, PlanDiagnostics Diagnostics) Build()
    {
        var plan = new PipelinePlan();
        var diagnostics = new PlanDiagnostics(log);

        foreach (var component in components)
        {
            log.Debug("Running plan component: {Component}", component.GetType().Name);
            component.Process(plan, diagnostics);
        }

        return (plan, diagnostics);
    }
}
