namespace Recyclarr.Pipelines.Plan;

internal class PlanBuilder(PipelineGraph graph, ILogger log)
{
    public PipelinePlan Build()
    {
        var plan = new PipelinePlan();

        foreach (var pipeline in graph.Pipelines)
        {
            log.Debug("Planning pipeline: {Pipeline}", pipeline.Type);
            pipeline.Plan(plan, graph);
        }

        return plan;
    }
}
