namespace Recyclarr.Pipelines.Plan;

/// <summary>
/// Plans one pipeline. Returns null when the instance configures nothing for the pipeline, which
/// skips its operation; outcomes reported through the context still count.
/// </summary>
internal interface IPipelinePlanner<out TPlan>
    where TPlan : class
{
    TPlan? Plan(PlanningContext context);
}
