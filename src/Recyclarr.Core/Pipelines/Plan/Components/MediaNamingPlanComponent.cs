namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Plans media naming for the instance's service. Sonarr and Radarr naming share no fields, so
/// all planning lives in the <see cref="IMediaNamingPlanner"/> that DI selects (ADR-023).
/// </summary>
internal class MediaNamingPlanComponent(IMediaNamingPlanner planner) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        if (planner.Plan(plan) is { } planned)
        {
            plan.MediaNaming = planned;
        }
    }
}

/// <summary>
/// Builds one service's planned media naming from its config and the guide. Returns null when
/// the instance configures no media naming. Invalid format references are added to the plan as
/// outcomes.
/// </summary>
internal interface IMediaNamingPlanner
{
    PlannedMediaNaming? Plan(PipelinePlan plan);
}
